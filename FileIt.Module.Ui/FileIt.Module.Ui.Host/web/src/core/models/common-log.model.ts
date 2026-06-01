export interface CommonLog {
  id: number;
  application: string;
  invocationId: string;
  eventName: string;
  createdOn: Date;
}

export interface CommonLogServer {
  id: number;
  application: string;
  invocationId: string;
  eventName: string;
  createdOn: string;
}

export function commonLogServerToCommonLog(item: CommonLogServer): CommonLog {
  return {
    ...item,
    createdOn: new Date(item.createdOn),
  };
}

export function commonLogsServerToCommonLogs(items: CommonLogServer[]): CommonLog[] {
  return items.map(commonLogServerToCommonLog);
}
