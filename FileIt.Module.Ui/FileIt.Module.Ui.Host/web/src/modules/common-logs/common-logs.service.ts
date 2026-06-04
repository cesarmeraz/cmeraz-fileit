import { buildQueryString, getJson } from '../../core/api/api-utils';
import {
  type CommonLog,
  type CommonLogServer,
  type CommonLogDetail,
  type CommonLogDetailServer,
  commonLogDetailsServerToCommonLogDetails,
  commonLogsServerToCommonLogs,
} from '../../core/models/common-log.model';

export async function getCommonLogs(application: string, timePeriod: string): Promise<CommonLog[]> {
  const queryString = buildQueryString({ application, timePeriod });
  const url = queryString ? `/common-logs?${queryString}` : '/common-logs';

  return getJson<CommonLogServer[]>(url).then((commonLogsServer: CommonLogServer[]) => {
    return commonLogsServerToCommonLogs(commonLogsServer);
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
