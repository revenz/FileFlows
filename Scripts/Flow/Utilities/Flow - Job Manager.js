/**
 * @description Manage concurrent execution of a flow.
 * @author blue5h1ft
 * @uid df4df16f-510e-49b8-a941-447e4f8beff7
 * @revision 1
 * @param {int} maxConcurrentJobs Number of concurrent jobs for this flow
 * @output Continue – Max concurrent jobs not reached, continue processing.
 * @output Skip – Max concurrent jobs reached, skipping. Use "Reprocess" plugin to add the file back to the queue and try again later.
 */
 
function randomSleep(minMs, maxMs) {
  var start = Date.now();
  var delay = minMs + Math.floor(Math.random() * (maxMs - minMs));
  while (Date.now() - start < delay) {}
}

// Fetch active jobs from FileFlows API and organize by flow name
function getActiveJobs() {
  var baseUrl = Variables.FileFlows.Url;
  var url = baseUrl + '/api/library-file?status=2';

  Logger.ILog(`Fetching data using curl from: ${url}`);

  const args = ['-s', '-X', 'GET', url];
  const result = Flow.Execute({
    command: 'curl',
    argumentList: args,
  });

  if (result.exitCode !== 0) {
    Logger.ELog(`curl command failed with exit code: ${result.exitCode}`);
    Logger.ELog(`Error output: ${result.standardError}`);

    return -1;
  }

  const responseText = result.output;

  try {
    const jobs = JSON.parse(responseText);
    const flowJobs = {};

    // Organize jobs by flow name, storing uid and processingStarted timestamp
    for (const job of jobs) {
      const flowName = job.FlowName;
      if (flowName) {
        if (!flowJobs[flowName]) {
          flowJobs[flowName] = [];
        }
        flowJobs[flowName].push({
          uid: job.Uid,
          processingStarted: job.ProcessingStarted ? Date.parse(job.ProcessingStarted) : null,
        });
      }
    }

    // Sort each flowName array by processingStarted, then uid
    for (const flowName in flowJobs) {
      flowJobs[flowName].sort((a, b) => {
        if (a.processingStarted === b.processingStarted) {
          return String(a.uid).localeCompare(String(b.uid));
        }
        return a.processingStarted - b.processingStarted;
      });
    }

    Logger.ILog(`Active flow jobs: ${JSON.stringify(flowJobs, null, 2)}`);

    return flowJobs;
  } catch (e) {
    Logger.ELog(`Failed to parse JSON response: ${e.message}`);
    Logger.WLog(`Received text: ${responseText}`);

    return -1;
  }
}

function Script(maxConcurrentJobs) {
  if (maxConcurrentJobs > 0) {
    // Sleep randomly to avoid race conditions
    randomSleep(500, 2000);

    var flowName = Variables.FlowName;
    var activeJobs = getActiveJobs();

    // If there are active jobs for this flow, check concurrency
    if (activeJobs && flowName && activeJobs[flowName] && Array.isArray(activeJobs[flowName])) {
      var currentUid = String(Flow.RunnerUid);

      // Get allowed Uids for this flow (first n=maxConcurrentJobs jobs)
      var jobsForFlow = activeJobs[flowName];
      var allowedUids = jobsForFlow.slice(0, maxConcurrentJobs).map((job) => String(job.uid));

      // If current Uid is not allowed, skip processing
      if (!allowedUids.includes(currentUid)) {
        Logger.ILog(`Maximum concurrent jobs (${maxConcurrentJobs}) reached for '${flowName}'. Skipping. Current Uid: ${currentUid}`);

        return 2;
      }
      Logger.ILog(`Starting job for '${flowName}'. Current Uid: '${currentUid}'`);
    }
  }

  return 1;
}
