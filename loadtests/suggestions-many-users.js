import http from "k6/http";
import { check, sleep } from "k6";
import { SharedArray } from "k6/data";

const userIds = new SharedArray("users", () => JSON.parse(open("./user-ids.json")));

export const options = {
  stages: [
    { duration: "30s", target: 10 },
    { duration: "1m", target: 20 },
    { duration: "30s", target: 0 },
  ],
  thresholds: { http_req_duration: ["p(95)<300"], http_req_failed: ["rate<0.01"] },
  summaryTrendStats: ["avg", "min", "med", "p(90)", "p(95)", "p(99)", "max"],
};

export default function () {
  const id = userIds[Math.floor(Math.random() * userIds.length)];
  const res = http.get(`${__ENV.API}/api/users/${id}/suggestions`);
  check(res, { "status is 200": (r) => r.status === 200 });
  sleep(1);
}