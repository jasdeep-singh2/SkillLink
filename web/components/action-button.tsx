"use client";

import { useFormStatus } from "react-dom";

// A one-button form that runs a Server Action. Works before JavaScript loads (it's a real <form>),
// and shows a pending label once it has.
export function ActionButton({ action, label, pendingLabel }: { action: () => Promise<void>; label: string; pendingLabel: string }) {
  return (
    <form action={action}>
      <SubmitButton label={label} pendingLabel={pendingLabel} />
    </form>
  );
}

function SubmitButton({ label, pendingLabel }: { label: string; pendingLabel: string }) {
  const { pending } = useFormStatus(); // only works in a component *inside* the <form>
  return (
    <button
      type="submit"
      disabled={pending}
      className="rounded-md border border-black/15 px-3 py-1 text-sm font-medium hover:bg-black/5 disabled:opacity-50 dark:border-white/20 dark:hover:bg-white/10"
    >
      {pending ? pendingLabel : label}
    </button>
  );
}
