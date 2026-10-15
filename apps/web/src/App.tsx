
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from './auth/useAuth';

export function App() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    void navigate('/login', { replace: true });
  };

  return (
    <div className="app">
      <header className="app__header">
        <Link className="app__brand" to="/tickets">
          Northwind Support
        </Link>

        <nav className="app__nav" aria-label="Main">
          <NavLink to="/tickets">Tickets</NavLink>
          <NavLink to="/customers">Customers</NavLink>
        </nav>

        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span>{user?.fullName}</span>
          <button
            className="button"
            type="button"
            onClick={handleLogout}
          >
            Log out
          </button>
        </div>
      </header>

      <main className="app__main">
        <Outlet />
      </main>
    </div>
  );
}
