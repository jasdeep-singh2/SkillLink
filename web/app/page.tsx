import { Suspense } from "react";
import { connection } from "next/server";

type Hello = { message: string };

async function HelloMessage() {
  await connection(); // run at request time, not build time

  const apiUrl = process.env.services__api__http__0;
  const res = await fetch(`${apiUrl}/api/hello`);
  if (!res.ok) {
    throw new Error(`API returned ${res.status}`);
  }
  const data: Hello = await res.json();

  return <h1 className="text-2xl font-bold">{data.message}</h1>;
}

export default function Home() {
  return (
    <main className="p-10">
      <Suspense fallback={<p>Loading…</p>}>
        <HelloMessage />
      </Suspense>
    </main>
  );
}