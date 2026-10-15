
import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './useAuth';
import { ApiError } from '../api/client';

export function LoginPage() {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const requestedPath = (
    location.state as { from?: string } | null
  )?.from;

  const destination =
    requestedPath?.startsWith('/') &&
    !requestedPath.startsWith('//') &&
    requestedPath !== '/login'
      ? requestedPath
      : '/tickets';

  if (isAuthenticated) {
    return <Navigate to={destination} replace />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setIsSubmitting(true);
    setError(null);

    try {
      await login({ email: email.trim(), password });
      void navigate(destination, { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError('Invalid email or password.');
      } else {
        setError(
          err instanceof Error
            ? err.message
            : 'Unable to sign in. Please try again.',
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="app__main">
      <div className="card" style={{ maxWidth: 440, margin: '64px auto' }}>
        <h1>Northwind Support</h1>
        <p className="muted">
          Sign in with your support agent account.
        </p>

        <form onSubmit={(event) => void handleSubmit(event)}>
          <div className="field">
            <label className="field__label" htmlFor="login-email">
              Email address
            </label>
            <input
              id="login-email"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="username"
              required
              disabled={isSubmitting}
            />
          </div>

          <div className="field">
            <label className="field__label" htmlFor="login-password">
              Password
            </label>
            <input
              id="login-password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              required
              disabled={isSubmitting}
            />
          </div>

          {error && (
            <p role="alert" className="form-error">
              {error}
            </p>
          )}

          <button
            className="button button--primary"
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </div>
    </main>
  );
}
