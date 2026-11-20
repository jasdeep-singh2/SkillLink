import { Suspense } from "react";
import { acceptRequest, sendRequest } from "@/app/actions/connections";
import { ActionButton } from "@/components/action-button";
import { ListSkeleton, PeopleList, PersonRow } from "@/components/person-row";
import { ApiError } from "@/lib/api";
import { getIncomingRequests, getSuggestions } from "@/lib/dal";

// The headings are the static shell (sent instantly); each list streams in behind its own boundary,
// so a slow suggestions query doesn't hold up the requests list.
export default function HomePage() {
  return (
    <main className="mx-auto flex w-full max-w-2xl flex-col gap-10 px-4 py-8">
      <section>
        <h2 className="text-lg font-semibold">Connection requests</h2>
        <Suspense fallback={<ListSkeleton />}>
          <IncomingRequests />
        </Suspense>
      </section>
      <section>
        <h2 className="text-lg font-semibold">People you may know</h2>
        <Suspense fallback={<ListSkeleton />}>
          <Suggestions />
        </Suspense>
      </section>
    </main>
  );
}

async function IncomingRequests() {
  const requests = await getIncomingRequests();
  return (
    <PeopleList empty="No pending requests.">
      {requests.map((r) => (
        <PersonRow key={r.userId} name={r.displayName} detail={`Sent ${new Date(r.sentAt).toLocaleDateString("en-GB")}`}>
          <ActionButton action={acceptRequest.bind(null, r.userId)} label="Accept" pendingLabel="Accepting…" />
        </PersonRow>
      ))}
    </PeopleList>
  );
}

async function Suggestions() {
  // Only the data fetch goes in try/catch: errors thrown while rendering JSX aren't caught here anyway.
  const result = await getSuggestions().catch((error: unknown) => {
    // The API's per-user rate limiter said no. Show it instead of crashing the page.
    if (error instanceof ApiError && error.status === 429) return error;
    throw error;
  });

  if (result instanceof ApiError)
    return <p className="py-3 text-sm">Too many requests. Try again in {result.retryAfterSeconds ?? 1} seconds.</p>;

  return (
    <PeopleList empty="No suggestions right now. Suggestions are people your connections know.">
      {result.map((s) => (
        <PersonRow key={s.id} name={s.displayName} detail={`${s.mutualCount} mutual connection${s.mutualCount === 1 ? "" : "s"}`}>
          <ActionButton action={sendRequest.bind(null, s.id)} label="Connect" pendingLabel="Sending…" />
        </PersonRow>
      ))}
    </PeopleList>
  );
}
