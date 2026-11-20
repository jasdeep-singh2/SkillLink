import { Suspense } from "react";
import { LoginForm } from "./login-form";

export const metadata = { title: "Log in · SkillLink" };

export default function LoginPage({ searchParams }: PageProps<"/login">) {
  return (
    <main className="mx-auto flex w-full max-w-sm flex-col gap-6 px-4 py-12">
      <h1 className="text-2xl font-bold">Log in</h1>
      {/* searchParams are request data, so this part streams in; the form is in the static shell. */}
      <Suspense fallback={null}>
        <ExpiredNotice searchParams={searchParams} />
      </Suspense>
      <LoginForm />
    </main>
  );
}

async function ExpiredNotice({ searchParams }: Pick<PageProps<"/login">, "searchParams">) {
  const { expired } = await searchParams;
  if (!expired) return null;
  return <p className="rounded-md bg-amber-100 px-3 py-2 text-sm text-amber-900">Your session expired. Please log in again.</p>;
}
