import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  stages: [
    { duration: "30s", target: 20 },  // ramp up to 20 virtual users
    { duration: "1m", target: 50 },   // then to 50
    { duration: "30s", target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_duration: ["p(95)<300"], // our Lesson 1 latency requirement
    http_req_failed: ["rate<0.01"],   // fewer than 1% errors
  },
};

const API = __ENV.API;
const USER_ID = __ENV.USER_ID;

export default function () {
  const res = http.get(`${API}/api/users/${USER_ID}/suggestions`);
  check(res, { "status is 200": (r) => r.status === 200 });
  sleep(1); // each virtual user waits 1 second between requests, like a real person
}