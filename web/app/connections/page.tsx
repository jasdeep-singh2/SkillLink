import { Suspense } from "react";
import { ListSkeleton, PeopleList, PersonRow } from "@/components/person-row";
import { getConnections } from "@/lib/dal";

export const metadata = { title: "Connections · SkillLink" };

export default function ConnectionsPage() {
  return (
    <main className="mx-auto flex w-full max-w-2xl flex-col gap-6 px-4 py-8">
      <h1 className="text-2xl font-bold">Your connections</h1>
      <Suspense fallback={<ListSkeleton />}>
        <ConnectionList />
      </Suspense>
    </main>
  );
}

async function ConnectionList() {
  const connections = await getConnections();
  return (
    <PeopleList empty="No connections yet. Find people to connect with.">
      {connections.map((c) => (
        <PersonRow key={c.userId} name={c.displayName} />
      ))}
    </PeopleList>
  );
}
