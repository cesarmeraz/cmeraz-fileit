import { getJson } from '../../core/api/api-client';
import {
  type CommonLog,
  type CommonLogServer,
  type CommonLogDetail,
  type CommonLogDetailServer,
  commonLogDetailsServerToCommonLogDetails,
  commonLogsServerToCommonLogs,
} from '../../core/models/common-log.model';

export async function getCommonLogs(): Promise<CommonLog[]> {
  return getJson<CommonLogServer[]>('/common-logs').then((commonLogsServer: CommonLogServer[]) => {
    const commonLogs = commonLogsServerToCommonLogs(commonLogsServer);
    return commonLogs;
  });
}

export async function getCommonLogDetails(invocationId: string): Promise<CommonLogDetail[]> {
  return getJson<CommonLogDetailServer[]>(`/common-log-details/${invocationId}`).then(
    (commonLogsServer: CommonLogDetailServer[]) => {
      const commonLogs = commonLogDetailsServerToCommonLogDetails(commonLogsServer);
      return commonLogs;
    },
  );
}
