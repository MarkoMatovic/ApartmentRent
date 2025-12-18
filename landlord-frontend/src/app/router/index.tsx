import { RouterProvider } from 'react-router-dom';
import { router } from './routes';

export const AppRouter = () => {
  return <RouterProvider router={router} />;
};

