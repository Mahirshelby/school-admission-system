import { BrowserRouter, Navigate, NavLink, Route, Routes } from 'react-router-dom';
import { ToastProvider } from './components/ToastProvider';
import { AdminPage } from './pages/AdminPage';
import { StudentPage } from './pages/StudentPage';

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <header className="topbar">
          <span className="brand">School Admission</span>
          <nav>
            <NavLink to="/student">Student</NavLink>
            <NavLink to="/admin">Admin</NavLink>
          </nav>
        </header>
        <main>
          <Routes>
            <Route path="/student" element={<StudentPage />} />
            <Route path="/admin" element={<AdminPage />} />
            <Route path="*" element={<Navigate to="/student" replace />} />
          </Routes>
        </main>
      </ToastProvider>
    </BrowserRouter>
  );
}
