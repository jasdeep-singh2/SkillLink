import Form from "next/form";
import { Suspense } from "react";
import { sendRequest } from "@/app/actions/connections";
import { ActionButton } from "@/components/action-button";
import { ListSkeleton, PeopleList, PersonRow } from "@/components/person-row";
import { searchUsers } from "@/lib/dal";

export const metadata = { title: "Find people · SkillLink" };

export default function PeoplePage({ searchParams }: PageProps<"/people">) {
  return (
    <main className="mx-auto flex w-full max-w-2xl flex-col gap-6 px-4 py-8">
      <h1 className="text-2xl font-bold">Find people</h1>
      {/* next/form: a GET form that navigates client-side to /people?q=... instead of reloading the page. */}
      <Form action="/people" className="flex gap-2">
        <input
          name="q"
          placeholder="Search by name"
          minLength={2}
          required
          aria-label="Search by name"
          className="flex-1 rounded-md border border-black/15 bg-transparent px-3 py-2 dark:border-white/20"
        />
        <button type="submit" className="rounded-md bg-foreground px-4 py-2 text-sm font-medium text-background">
          Search
        </button>
      </Form>
      <Suspense fallback={<ListSkeleton />}>
        <Results searchParams={searchParams} />
      </Suspense>
    </main>
  );
}

async function Results({ searchParams }: Pick<PageProps<"/people">, "searchParams">) {
  const { q } = await searchParams;
  const query = typeof q === "string" ? q.trim() : "";
  if (query.length < 2) return null;

  const people = await searchUsers(query);
  return (
    <PeopleList empty={`Nobody found for "${query}".`}>
      {people.map((p) => (
        <PersonRow key={p.id} name={p.displayName} detail={p.headline}>
          <ActionButton action={sendRequest.bind(null, p.id)} label="Connect" pendingLabel="Sending…" />
        </PersonRow>
      ))}
    </PeopleList>
  );
}
