// Verifies mid-run cancellation: starts a generation, cancels after the first completed
// region, and asserts the response arrives with stoppedEarly=true and partial output saved.
// Usage: node cancel-test.mjs <definitionPath>
import { spawn } from "node:child_process";
import { createInterface } from "node:readline";
import { existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const exe = ["Release", "Debug"]
  .map(c => join(here, "Vali.Host", "bin", c, "net10.0", "Vali.Host.exe"))
  .find(existsSync);
if (!exe) {
  console.error("Vali.Host.exe not found — run: dotnet build studio/sidecar/Vali.Host -c Release");
  process.exit(1);
}

const definitionPath = process.argv[2];
if (!definitionPath) {
  console.error("Usage: node cancel-test.mjs <definitionPath>");
  process.exit(1);
}

const child = spawn(exe, [], { stdio: ["pipe", "pipe", "inherit"] });
const rl = createInterface({ input: child.stdout });
let cancelSent = false;
let regionsBeforeCancel = 0;

child.stdin.write(JSON.stringify({ id: "gen", cmd: "generate", params: { definitionPath } }) + "\n");

const timeout = setTimeout(() => {
  console.error("FAIL: timed out");
  child.kill();
  process.exit(1);
}, 120_000);

rl.on("line", line => {
  const msg = JSON.parse(line);
  if (msg.event === "regionCompleted" && !cancelSent) {
    regionsBeforeCancel++;
    cancelSent = true;
    child.stdin.write(JSON.stringify({ id: "c1", cmd: "cancel", params: { targetId: "gen" } }) + "\n");
    console.log(`cancel sent after region ${msg.data.subdivisionCode}`);
  } else if (msg.id === "c1") {
    console.log(`cancel response: ${JSON.stringify(msg)}`);
  } else if (msg.id === "gen" && msg.ok !== undefined) {
    clearTimeout(timeout);
    const r = msg.result;
    if (msg.ok && r.stoppedEarly && r.locationsWritten > 0 && existsSync(r.outputPath)) {
      console.log(`PASS: stoppedEarly=${r.stoppedEarly}, saved ${r.locationsWritten} locations to ${r.outputPath}`);
      child.stdin.end();
      process.exit(0);
    }
    console.error(`FAIL: unexpected final response: ${line.substring(0, 300)}`);
    child.stdin.end();
    process.exit(1);
  }
});
