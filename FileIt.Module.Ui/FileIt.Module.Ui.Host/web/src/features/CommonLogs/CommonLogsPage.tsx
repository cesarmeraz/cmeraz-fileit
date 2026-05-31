import { useEffect, useState } from 'react';
import Page from '../../common/Page';
import PageTitle from '../../common/PageTitle';
import type { CommonLog } from './models';
import logData from '../../data/log-query-highlevel-output.json';
import { CommonLogsGrid } from './CommonLogsGrid';

const CommonLogsPage: React.FC = () => {
  const [commonLogs, setCommonLogs] = useState<CommonLog[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    let cancelled = false;
    const doGetCommonLogs = async () => {
      if (!cancelled) {
        setCommonLogs(logData);
        setLoading(false);
      }
    };
    doGetCommonLogs();
    return () => {
      cancelled = true;
    };
  }, [commonLogs]);

  return (
    <Page>
      <PageTitle>Logs</PageTitle>
      {loading ? (
        <div>Loading...</div>
      ) : (
        <CommonLogsGrid data={commonLogs}></CommonLogsGrid>
      )}
      <div></div>
    </Page>
  );
};

export default CommonLogsPage;
