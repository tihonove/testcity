import React from 'react';
import { Issue } from '../types/Issue';
import { diff } from './utils';

interface OverviewDiffProps {
  baseline: Issue[];
  current: Issue[];
}

export function OverviewDiff({ baseline, current }: OverviewDiffProps) {
  const { fixed, added } = diff(baseline, current);
  console.info(fixed);
  console.info(added);
  // diff by fingerprint
  return <div></div>;
}
