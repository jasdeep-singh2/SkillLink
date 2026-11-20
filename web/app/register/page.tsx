import { RegisterForm } from "./register-form";

export const metadata = { title: "Create account · SkillLink" };

export default function RegisterPage() {
  return (
    <main className="mx-auto flex w-full max-w-sm flex-col gap-6 px-4 py-12">
      <h1 className="text-2xl font-bold">Create your account</h1>
      <RegisterForm />
    </main>
  );
}
