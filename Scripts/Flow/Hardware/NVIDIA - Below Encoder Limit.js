/**
 * @author reven
 * @uid 071ef14f-46db-4e21-b438-30ac56b37cc4
 * @description Checks the count of NVIDIA encodes currently processing and see if it is below a limit
 * @revision 4
 * @param {int} EncoderLimit The maximum number of encoders available
 * @output Below encoder limit
 * @output Not below encoder limit
 */
function Script(EncoderLimit)
{
  // nvidia-smi --query-gpu=encoder.stats.sessionCount --format=csv
  let process = Flow.Execute({
    command: 'nvidia-smi',
    argumentList: [
      '--query-gpu=encoder.stats.sessionCount',
      '--format=csv,noheader'
    ]
  });

  if (process.exitCode != 0)
  {
    Logger.ELog('Unable to execute nvidia-smi');
    return -1;
  }

  let encoders = parseInt(process.standardOutput, 10);
  if (isNaN(encoders))
  {
    Logger.ELog('Unable to parse number of encoders: ' + process.standardOutput);
    return 1;
  }

  if (encoders >= EncoderLimit)
  {
    Logger.WLog('Is not below the limit, at: ' + encoders);
    return 2;
  }

  Logger.ILog('Is below the limit, at: ' + encoders);
  return 1;
}