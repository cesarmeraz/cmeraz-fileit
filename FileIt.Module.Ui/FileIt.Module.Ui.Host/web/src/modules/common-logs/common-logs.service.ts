import { getJson } from '../../core/api/api-client';
import logData from '../../data/log-query-highlevel-output.json';
import { commonLogsServerToCommonLogs, type CommonLog, type CommonLogServer } from '../../core/models/common-log.model';

export async function getCommonLogs(): Promise<CommonLog[]> {
  // return getJson<CommonLogServer[]>('/common-logs').then((commonLogsServer: CommonLogServer[]) => {
  //   const commonLogs = commonLogsServerToCommonLogs(logData);
  //   return commonLogs;
  // });

  const commonLogs = commonLogsServerToCommonLogs(logData);
  return commonLogs;
}
