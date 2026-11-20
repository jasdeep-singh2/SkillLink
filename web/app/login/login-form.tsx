"use client";

import Link from "next/link";
import { useActionState } from "react";
import { login } from "@/app/actions/auth";
import { buttonClass, FormField } from "@/components/form-field";

export function LoginForm() {
  // state: what the Server Action returned last time (errors, values to refill); pending: request in flight.
  const [state, action, pending] = useActionState(login, undefined);

  return (
    <form action={action} className="flex flex-col gap-4">
      {state?.error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {state.error}
        </p>
      )}
      <FormField label="Email" name="email" type="email" required autoComplete="email" defaultValue={state?.values?.email} />
      <FormField label="Password" name="password" type="password" required autoComplete="current-password" />
      <button type="submit" disabled={pending} className={buttonClass}>
        {pending ? "Logging in…" : "Log in"}
      </button>
      <p className="text-sm">
        No account yet?{" "}
        <Link href="/register" className="underline">
          Create one
        </Link>
      </p>
    </form>
  );
}
