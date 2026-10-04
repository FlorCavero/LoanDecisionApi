import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { Layout, RequireAuth } from "./components/Layout";
import { LoginPage } from "./pages/LoginPage";
import { LoanApplicationPage } from "./pages/LoanApplicationPage";
import { ApprovalPage } from "./pages/ApprovalPage";

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/login" element={<LoginPage />} />
            <Route
              path="/apply"
              element={
                <RequireAuth>
                  <LoanApplicationPage />
                </RequireAuth>
              }
            />
            <Route
              path="/evaluate"
              element={
                <RequireAuth>
                  <ApprovalPage />
                </RequireAuth>
              }
            />
            <Route path="*" element={<Navigate to="/apply" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
