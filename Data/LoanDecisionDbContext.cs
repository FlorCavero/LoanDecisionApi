using System.Text.Json;
using System.Text.Json.Serialization;
using LoanDecisionApi.Models.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanDecisionApi.Data;

public class LoanDecisionDBContext(DbContextOptions<LoanDecisionDBContext> dbContext,
    IDataProtectionProvider dataProtectionProvider) : DbContext(dbContext)
{
    public DbSet<LoanApplication> LoanApplications {get; set;}
    public DbSet<User> Users {get; set;}
    public DbSet<ApiPartner> ApiPartners {get; set;}
    public DbSet<RuleGroup> RuleGroups {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        var protector = dataProtectionProvider.CreateProtector("LoanApplication.Ssn");
        var ssnConverter = new ValueConverter<string, string>(
        plaintext => protector.Protect(plaintext),
        ciphertext => protector.Unprotect(ciphertext));

        modelBuilder.Entity<LoanApplication>()
            .ToTable("loan_applications")
            .HasKey(x => x.Id);
        modelBuilder.Entity<LoanApplication>()
            .Property(x => x.CreatedAt);
        modelBuilder.Entity<LoanApplication>()
            .Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<LoanApplication>()
            .Property(x => x.DelinquencyStatus).HasConversion<string>();
        modelBuilder.Entity<LoanApplication>()
            .Property(x => x.Ssn).HasConversion(ssnConverter);
        modelBuilder.Entity<LoanApplication>()
            .Ignore(x => x.DebtToIncomeRatio);

        var ruleTreeJsonOptions = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };

        var rulesConverter = new ValueConverter<List<Rule>, string>(
            rules => JsonSerializer.Serialize(rules, ruleTreeJsonOptions),
            json => JsonSerializer.Deserialize<List<Rule>>(json, ruleTreeJsonOptions) ?? new List<Rule>());

        var childGroupsConverter = new ValueConverter<List<RuleGroup>, string>(
            groups => JsonSerializer.Serialize(groups, ruleTreeJsonOptions),
            json => JsonSerializer.Deserialize<List<RuleGroup>>(json, ruleTreeJsonOptions) ?? new List<RuleGroup>());

        // Rule/RuleGroup don't implement value equality, so the change tracker can't tell
        // whether two List<Rule>/List<RuleGroup> instances represent the same content
        // (e.g. after a fresh deserialize) unless told explicitly. Comparing via the same
        // serialized JSON that's actually written to the column is the most direct way to
        // answer "did this really change" without needing Rule/RuleGroup to implement it.
        var rulesComparer = new ValueComparer<List<Rule>>(
            (a, b) => JsonSerializer.Serialize(a, ruleTreeJsonOptions) == JsonSerializer.Serialize(b, ruleTreeJsonOptions),
            v => JsonSerializer.Serialize(v, ruleTreeJsonOptions).GetHashCode(),
            v => JsonSerializer.Deserialize<List<Rule>>(JsonSerializer.Serialize(v, ruleTreeJsonOptions), ruleTreeJsonOptions)!);

        var childGroupsComparer = new ValueComparer<List<RuleGroup>>(
            (a, b) => JsonSerializer.Serialize(a, ruleTreeJsonOptions) == JsonSerializer.Serialize(b, ruleTreeJsonOptions),
            v => JsonSerializer.Serialize(v, ruleTreeJsonOptions).GetHashCode(),
            v => JsonSerializer.Deserialize<List<RuleGroup>>(JsonSerializer.Serialize(v, ruleTreeJsonOptions), ruleTreeJsonOptions)!);

        modelBuilder.Entity<RuleGroup>()
            .ToTable("rule_groups")
            .HasKey(x => x.Id);
        modelBuilder.Entity<RuleGroup>()
            .Property(x => x.CreatedAt);
        modelBuilder.Entity<RuleGroup>()
            .Property(x => x.Grouping).HasConversion<string>();
        modelBuilder.Entity<RuleGroup>()
            .Property(x => x.Rules).HasConversion(rulesConverter, rulesComparer).HasColumnType("jsonb");
        modelBuilder.Entity<RuleGroup>()
            .Property(x => x.ChildGroups).HasConversion(childGroupsConverter, childGroupsComparer).HasColumnType("jsonb");

        modelBuilder.Entity<User>()
            .ToTable("users")
            .HasKey(x => x.Id);
        modelBuilder.Entity<User>()
            .Property(x => x.CreatedAt);

        modelBuilder.Entity<ApiPartner>()
            .ToTable("api_partners")
            .HasKey(x => x.Id);
        modelBuilder.Entity<ApiPartner>()
            .Property(x => x.CreatedAt);
        modelBuilder.Entity<ApiPartner>()
            .HasIndex(x => x.Name)
            .IsUnique();
    }
}
