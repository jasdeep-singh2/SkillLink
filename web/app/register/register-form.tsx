"use client";

import Link from "next/link";
import { useActionState } from "react";
import { register } from "@/app/actions/auth";
import { buttonClass, FormField } from "@/components/form-field";

export function RegisterForm() {
  const [state, action, pending] = useActionState(register, undefined);
  const errors = state?.fieldErrors;

  return (
    <form action={action} className="flex flex-col gap-4">
      <FormField label="Name" name="displayName" required autoComplete="name" defaultValue={state?.values?.displayName} errors={errors?.displayname} />
      <FormField label="Email" name="email" type="email" required autoComplete="email" defaultValue={state?.values?.email} errors={errors?.email} />
      <FormField label="Headline (optional)" name="headline" defaultValue={state?.values?.headline} errors={errors?.headline} />
      <FormField label="Password (at least 8 characters)" name="password" type="password" required autoComplete="new-password" errors={errors?.password} />
      <button type="submit" disabled={pending} className={buttonClass}>
        {pending ? "Creating account…" : "Create account"}
      </button>
      <p className="text-sm">
        Already have an account?{" "}
        <Link href="/login" className="underline">
          Log in
        </Link>
      </p>
    </form>
  );
}
