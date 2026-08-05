// Refuse a file operation on a path inside the project that resolves outside it.
//
// A path pointing outside the project is left alone; asking for one is deliberate. What this
// blocks is a path that looks local and is not — a symlink in the working tree leading to a
// file elsewhere on the machine. The pipeline makes that reachable: .claude/runs is a writable
// mount into the container, and the agents writing there review diffs written by people
// outside this project.
import { readFileSync, realpathSync } from "node:fs";
import { resolve, relative, isAbsolute } from "node:path";

const projectDir = realpathSync(process.env.CLAUDE_PROJECT_DIR ?? process.cwd());

const inside = (child) => {
  const rel = relative(projectDir, child);
  return rel !== "" && !rel.startsWith("..") && !isAbsolute(rel);
};

// The nearest ancestor that exists, so a file about to be created is judged by the directory
// it lands in.
const resolveExisting = (path) => {
  for (let current = path; ; ) {
    try {
      return realpathSync(current);
    } catch {
      const parent = resolve(current, "..");
      if (parent === current) {
        return null;
      }
      current = parent;
    }
  }
};

let input;
try {
  input = JSON.parse(readFileSync(0, "utf8"));
} catch {
  process.exit(0);
}

const filePath = input?.tool_input?.file_path ?? input?.tool_input?.notebook_path;
if (!filePath) {
  process.exit(0);
}

const target = resolve(projectDir, filePath);
if (!inside(target)) {
  process.exit(0);
}

const resolved = resolveExisting(target);
if (resolved !== null && !inside(resolved)) {
  console.error(
    `${filePath} is inside the project but resolves to ${resolved}. ` +
      "Refusing to follow it out."
  );
  process.exit(2);
}
