import Link from "next/link";
import { logout } from "@/app/actions/auth";
import { tryGetCurrentUser } from "@/lib/dal";

// Reads the session cookie, so it must sit inside <Suspense> (see layout.tsx).
export async function UserMenu() {
  const user = await tryGetCurrentUser();

  if (!user)
    return (
      <Link href="/login" className="text-sm underline">
        Log in
      </Link>
    );

  return (
    <div className="flex items-center gap-3 text-sm">
      <span className="opacity-70">{user.displayName}</span>
      <form action={logout}>
        <button type="submit" className="underline">
          Log out
        </button>
      </form>
    </div>
  );
}
