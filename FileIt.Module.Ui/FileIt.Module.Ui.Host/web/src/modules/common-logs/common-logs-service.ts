import { getJson } from '../../core/api/api-client';
import { commonLogsServerToCommonLogs, type CommonLog, type CommonLogServer } from '../../core/models/common-log.model';

export async function getCommonLogs(): Promise<CommonLog[]> {
  return getJson<CommonLogServer[]>('/common-logs').then((commonLogsServer: CommonLogServer[]) => {
    const commonLogs = commonLogsServerToCommonLogs(commonLogsServer);
    return commonLogs;
  });
}
