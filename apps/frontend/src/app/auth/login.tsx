import { ChapiLogo } from '@/components/ChapiLogo';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAuth } from '@/lib/auth/AuthContext';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Lock, Mail } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { z } from 'zod';

const schema = z.object({
  username: z.string().min(1, 'Username is required'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});
type FormValues = z.infer<typeof schema>;

export default function Login() {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });
  const [showPw, setShowPw] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isInitializing, setIsInitializing] = useState(true);
  const { login, isAuthenticated, isLoading: authLoading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  // Get the originally requested URL or default to dashboard
  const from = (location.state as any)?.from?.pathname || '/app';

  // Handle initial auth check loading
  useEffect(() => {
    if (!authLoading) {
      setIsInitializing(false);
    }
  }, [authLoading]);

  // If already authenticated, redirect to dashboard
  useEffect(() => {
    if (!isInitializing && isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, isInitializing, navigate, from]);

  // Show loading if auth is still loading or initializing
  if (isInitializing || authLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <div className="flex flex-col items-center space-y-4">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
          <p className="text-sm text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  const onSubmit = async (data: FormValues) => {
    if (isLoading) return; // Prevent multiple submissions

    setIsLoading(true);
    try {
      const result = await login({
        email: data.username,
        password: data.password,
      } as any);

      if (result.success) {
        toast.success('Logged in successfully');
        // Redirect to the originally requested page or dashboard
        navigate(from, { replace: true });
      } else {
        toast.error(
          result.errorMessage || 'Login failed. Please check your credentials.'
        );
      }
    } catch (error) {
      console.error('Login error:', error);
      const errorMessage =
        error instanceof Error ? error.message : 'An unexpected error occurred';
      toast.error(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex flex-col min-h-screen items-center justify-center bg-gray-50 gap-6 py-12">
      <Card className="w-full max-w-md">
        <CardHeader>
          <div className="text-center">
            <div className="flex justify-center">
              <ChapiLogo size={48} />
            </div>
            <h2 className="mt-6 text-3xl font-bold text-gray-900 dark:text-white">
              Welcome back
            </h2>
            <p className="mt-2 text-sm text-gray-600 dark:text-gray-300">
              Sign in to your Chapi account
            </p>
          </div>
          <CardTitle>Sign In</CardTitle>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
            <div className="space-y-2">
              <Label htmlFor="username">Username</Label>
              <div className="relative">
                <Mail className="absolute left-3 top-2.5 h-4 w-4 text-gray-400" />
                <Input
                  id="username"
                  aria-label="Username"
                  type="text"
                  placeholder="Enter your username"
                  {...register('username')}
                  className="pl-10"
                  autoFocus
                />
                {errors.username && (
                  <p className="text-red-500 text-sm">
                    {errors.username.message}
                  </p>
                )}
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-2.5 h-4 w-4 text-gray-400" />
                <Input
                  id="password"
                  aria-label="Password"
                  type={showPw ? 'text' : 'password'}
                  placeholder="Enter your password"
                  {...register('password')}
                  className="pl-10 pr-10"
                />
                <button
                  type="button"
                  tabIndex={-1}
                  aria-label={showPw ? 'Hide password' : 'Show password'}
                  className="absolute right-3 top-2.5 text-gray-400"
                  onClick={() => setShowPw(v => !v)}
                >
                  {showPw ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
                {errors.password && (
                  <p className="text-red-500 text-sm">
                    {errors.password.message}
                  </p>
                )}
              </div>
            </div>
            <div className="flex items-center justify-end">
              <Link
                to="/auth/forgot"
                className="text-sm text-blue-500 hover:underline"
              >
                Forgot password?
              </Link>
            </div>
            <Button
              type="submit"
              className="w-full bg-blue-500 hover:bg-blue-600 text-white rounded-xl"
              disabled={isLoading}
            >
              {isLoading ? 'Logging in...' : 'Sign In'}
            </Button>
          </form>
          <div className="flex justify-between text-sm mt-2">
            <span></span>
            <Link to="/auth/signup" className="text-blue-500 hover:underline">
              Create account
            </Link>
          </div>
        </CardContent>
      </Card>
      <div className="w-full max-w-md mt-6 text-center text-sm text-muted-foreground">
        <div className="flex items-center justify-center gap-2">
          <ChapiLogo size={20} />
          <div>© {new Date().getFullYear()} Chapi. All rights reserved.</div>
        </div>
      </div>
    </div>
  );
}
