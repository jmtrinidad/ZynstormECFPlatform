// QuestPDF's first font access (via the RI templates) can race under xUnit's parallel
// test execution, causing an intermittent font-init failure. Rendering is fast, so run
// the test collections sequentially to keep the suite deterministic.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
