import type { ReactNode } from 'react';

interface MainLayoutProps {
  children: ReactNode;
}

export function MainLayout({ children }: MainLayoutProps) {
  return (
    <main className="flex h-screen gap-4 p-4 flex-col md:flex-row">
      {children}
    </main>
  );
}
