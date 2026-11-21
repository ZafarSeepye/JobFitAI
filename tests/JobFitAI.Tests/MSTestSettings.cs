// Run test methods in parallel. Each API test creates its own in-memory app,
// so tests never share rate-limit counters.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
