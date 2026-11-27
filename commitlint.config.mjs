// Conventional Commits with the scopes listed in CLAUDE.md. Enforced in CI (commit-messages job).
export default {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-empty': [2, 'never'],
    'scope-enum': [
      2,
      'always',
      [
        'repo', 'compose', 'platform-api', 'assets', 'maintenance', 'inventory', 'audit', 'ingest', 'ml',
        'simulator', 'web', 'contracts', 'infra', 'ci', 'docs', 'adr', 'plant', 'deps', 'learning',
        'blueprint', 'security', 'authz',
      ],
    ],
    'header-max-length': [2, 'always', 100],
    'body-max-line-length': [1, 'always', 120],
  },
  // Why the Dependabot rule: its titles start with a capital ("chore(deps): Bump x"), which subject-case rejects,
  // and the wording is not ours to change. Our own commits are still linted in full.
  ignores: [
    (message) => /^(Merge |Revert )/.test(message),
    (message) => message.includes('Signed-off-by: dependabot[bot]'),
  ],
};
