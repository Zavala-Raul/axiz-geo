import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { ApiError, USE_MOCKS } from '../../../shared/api/httpClient';
import { LoginForm } from '../components/LoginForm';
import { useAuth } from '../hooks/useAuth';
import { DEMO_ACCOUNTS } from '../mocks/authMock';
import type { DemoAccountOption } from '../types';
import { getDefaultRouteForRole } from '../utils/roleRoutes';

export function LoginPage() {
  const { isAuthenticated, user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState<string>('monitor@axiz.demo');
  const [password, setPassword] = useState<string>('Demo123!');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (isAuthenticated && user) {
    return <Navigate to={getDefaultRouteForRole(user.role)} replace />;
  }

  const handleSelectDemoAccount = (account: DemoAccountOption) => {
    setEmail(account.email);
    setPassword('Demo123!');
    setErrorMessage(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      const loggedInUser = await login({ email, password });
      const state = location.state as { from?: string } | null;
      const destination =
        state?.from && state.from !== '/login'
          ? state.from
          : getDefaultRouteForRole(loggedInUser.role);
      navigate(destination, { replace: true });
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setErrorMessage(err.message);
      } else {
        setErrorMessage('No fue posible conectar con el servicio de autenticación.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <LoginForm
      email={email}
      password={password}
      isSubmitting={isSubmitting}
      errorMessage={errorMessage}
      useMocks={USE_MOCKS}
      demoAccounts={DEMO_ACCOUNTS}
      onEmailChange={setEmail}
      onPasswordChange={setPassword}
      onSubmit={handleSubmit}
      onSelectDemoAccount={handleSelectDemoAccount}
    />
  );
}
