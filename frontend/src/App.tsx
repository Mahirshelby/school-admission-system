import { BrowserRouter, Navigate, NavLink, Route, Routes } from 'react-router-dom';
import { ToastProvider } from './components/ToastProvider';
import { AdminPage } from './pages/AdminPage';

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <header className="topbar">
          <span className="brand">School Admission</span>
          <nav>
            <NavLink to="/admin">Admin</NavLink>
          </nav>
        </header>
        <main>
          <Routes>
            <Route path="/admin" element={<AdminPage />} />
            <Route path="*" element={<Navigate to="/admin" replace />} />
          </Routes>
        </main>
      </ToastProvider>
    </BrowserRouter>
  );
}
