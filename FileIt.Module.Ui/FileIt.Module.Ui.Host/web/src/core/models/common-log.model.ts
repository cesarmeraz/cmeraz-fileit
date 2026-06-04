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

export interface CommonLogDetail {
  id: number;
  message: string;
  level: string;
  exception: string;
  environment: string;
  machineName: string;
  application: string;
  applicationVersion: string;
  infrastructureVersion: string;
  sourceContext: string;
  correlationId: string;
  invocationId: string;
  eventName: string;
  createdOn: Date;
}

export interface CommonLogDetailServer {
  id: number;
  message: string;
  level: string;
  exception: string;
  environment: string;
  machineName: string;
  application: string;
  applicationVersion: string;
  infrastructureVersion: string;
  sourceContext: string;
  correlationId: string;
  invocationId: string;
  eventName: string;
  createdOn: Date;
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

export function commonLogDetailServerToCommonLogDetail(item: CommonLogDetailServer): CommonLogDetail {
  return {
    ...item,
    createdOn: new Date(item.createdOn),
  };
}

export function commonLogDetailsServerToCommonLogDetails(items: CommonLogDetailServer[]): CommonLogDetail[] {
  return items.map(commonLogDetailServerToCommonLogDetail);
}
