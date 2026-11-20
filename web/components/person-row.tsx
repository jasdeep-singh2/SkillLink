import type { ReactNode } from "react";

export function PersonRow({ name, detail, children }: { name: string; detail?: string | null; children?: ReactNode }) {
  return (
    <li className="flex items-center justify-between gap-4 py-3">
      <div className="min-w-0">
        <p className="truncate font-medium">{name}</p>
        {detail && <p className="truncate text-sm opacity-70">{detail}</p>}
      </div>
      {children}
    </li>
  );
}

export function PeopleList({ children, empty }: { children: ReactNode[]; empty: string }) {
  if (children.length === 0) return <p className="py-3 text-sm opacity-70">{empty}</p>;
  return <ul className="divide-y divide-black/10 dark:divide-white/10">{children}</ul>;
}

export function ListSkeleton() {
  return <p className="py-3 text-sm opacity-50">Loading…</p>;
}
