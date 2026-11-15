import http from "k6/http";
import { check } from "k6";

export const options = {
  scenarios: {
    burst: { executor: "constant-arrival-rate", rate: 30, timeUnit: "1s", duration: "20s", preAllocatedVUs: 20 },
  },
};

export default function () {
  const res = http.get(`${__ENV.API}/api/users/${__ENV.USER_ID}/suggestions`);
  check(res, {
    "200 allowed": (r) => r.status === 200,
    "429 rate limited": (r) => r.status === 429,
  });
}