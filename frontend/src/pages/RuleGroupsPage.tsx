import { useEffect, useState, type FormEvent } from "react";
import { useAuth } from "../context/AuthContext";
import { listRuleGroups, createRuleGroup, ApiError } from "../api/client";
import {
  DATA_POINTS,
  type RuleGroupResponse,
  type RuleCreateRequest,
  type RuleCondition,
  type RuleGrouping,
} from "../api/types";

const CONDITIONS: RuleCondition[] = [
  "Equals",
  "DoesNotEqual",
  "GreaterThan",
  "GreaterOrEqualTo",
  "LessThan",
  "LessOrEqualTo",
  "In",
  "NotIn",
];

function emptyRule(): RuleCreateRequest {
  return { dataPoint: DATA_POINTS[0], condition: "GreaterOrEqualTo", value: "" };
}

// RuleGroups can nest child groups arbitrarily deep (mirrors RuleGroupResultView
// on the evaluation page) - this UI can only ever *display* that nesting, since
// the create form below only supports a single flat level of rules.
function RuleGroupView({ group }: { group: RuleGroupResponse }) {
  return (
    <div className="rule-group">
      <div className="rule-group-header">
        <strong>{group.name}</strong>
        <span className="grouping-label">{group.grouping} of:</span>
      </div>

      {group.rules.length > 0 && (
        <ul className="rule-list">
          {group.rules.map((rule) => (
            <li key={rule.id}>
              <code>{rule.dataPoint}</code> {rule.condition} <code>{rule.value}</code>
            </li>
          ))}
        </ul>
      )}

      {group.childGroups.length > 0 && (
        <div className="child-groups">
          {group.childGroups.map((child) => (
            <RuleGroupView key={child.id} group={child} />
          ))}
        </div>
      )}
    </div>
  );
}

export function RuleGroupsPage() {
  const { token } = useAuth();

  const [ruleGroups, setRuleGroups] = useState<RuleGroupResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [grouping, setGrouping] = useState<RuleGrouping>("And");
  const [rules, setRules] = useState<RuleCreateRequest[]>([emptyRule()]);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function loadRuleGroups() {
    setIsLoading(true);
    setListError(null);
    try {
      setRuleGroups(await listRuleGroups(token!));
    } catch (err) {
      setListError(err instanceof ApiError ? err.message : "Unable to reach the API.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadRuleGroups();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function updateRule(index: number, patch: Partial<RuleCreateRequest>) {
    setRules((current) => current.map((rule, i) => (i === index ? { ...rule, ...patch } : rule)));
  }

  function removeRule(index: number) {
    setRules((current) => current.filter((_, i) => i !== index));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setFormError(null);
    setIsSubmitting(true);
    try {
      await createRuleGroup(token!, { name, grouping, rules, childGroups: [] });
      setName("");
      setGrouping("And");
      setRules([emptyRule()]);
      await loadRuleGroups();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Unable to reach the API.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="page">
      <h1>Rule Groups</h1>

      <div className="two-column">
        <form className="card" onSubmit={handleSubmit}>
          <h2>New rule group</h2>

          <label>
            Name
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </label>

          <label>
            Grouping
            <select value={grouping} onChange={(e) => setGrouping(e.target.value as RuleGrouping)}>
              <option value="And">And - every rule must pass</option>
              <option value="Or">Or - any one rule passing is enough</option>
            </select>
          </label>

          <div className="rule-rows">
            {rules.map((rule, index) => (
              <div className="rule-row" key={index}>
                <div className="rule-row-header">
                  <span className="rule-row-label">Rule {index + 1}</span>
                  {rules.length > 1 && (
                    <button type="button" className="remove-rule" onClick={() => removeRule(index)}>
                      Remove
                    </button>
                  )}
                </div>

                <label>
                  Data point
                  <select
                    value={rule.dataPoint}
                    onChange={(e) => updateRule(index, { dataPoint: e.target.value })}
                  >
                    {DATA_POINTS.map((dp) => (
                      <option key={dp} value={dp}>
                        {dp}
                      </option>
                    ))}
                  </select>
                </label>

                <label>
                  Condition
                  <select
                    value={rule.condition}
                    onChange={(e) => updateRule(index, { condition: e.target.value as RuleCondition })}
                  >
                    {CONDITIONS.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </label>

                <label>
                  Value
                  <textarea
                    className="rule-value"
                    rows={2}
                    value={rule.value}
                    onChange={(e) => updateRule(index, { value: e.target.value })}
                    placeholder={rule.condition === "In" || rule.condition === "NotIn" ? "e.g. Current,Days30" : "e.g. 640"}
                    required
                  />
                </label>
              </div>
            ))}
          </div>

          <button type="button" className="link-button" onClick={() => setRules((r) => [...r, emptyRule()])}>
            + Add another rule
          </button>

          {formError && <p className="error">{formError}</p>}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Creating..." : "Create rule group"}
          </button>
        </form>

        <div className="card result-card">
          <h2>Existing rule groups</h2>
          {isLoading && <p className="muted">Loading...</p>}
          {listError && <p className="error">{listError}</p>}
          {!isLoading && !listError && ruleGroups.length === 0 && (
            <p className="muted">No rule groups yet.</p>
          )}
          {ruleGroups.map((group) => (
            <RuleGroupView key={group.id} group={group} />
          ))}
        </div>
      </div>
    </div>
  );
}
