export const APPLICATION_MAPPINGS: Record<string, string> = {
  '': 'All',
  'FileIt.Module.DataFlow.Host': 'DataFlow',
  'FileIt.Module.SimpleFlow.Host': 'SimpleFlow',
  'FileIt.Module.ComplexFlow.Host': 'ComplexFlow',
};

export function getFriendlyName(longName: string): string {
  return APPLICATION_MAPPINGS[longName] || longName;
}

export function getLongName(friendlyName: string): string {
  const entry = Object.entries(APPLICATION_MAPPINGS).find(([, friendly]) => friendly === friendlyName);
  return entry ? entry[0] : friendlyName;
}

export function getAllApplicationMappings(): Array<{ long: string; friendly: string }> {
  return Object.entries(APPLICATION_MAPPINGS).map(([long, friendly]) => ({
    long,
    friendly,
  }));
}
