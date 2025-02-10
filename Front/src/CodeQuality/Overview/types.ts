import { Content } from '../types/Content';
import { Severity } from '../types/Severity';

export type SeverityOverview = {
  total: number;
  issues: Record<string, IssueOverview>;
};

export type IssueOverview = {
  count: number;
  content?: Content;
  categories?: string[];
};

export type DiffOverview = {
  fixed: Record<Severity, SeverityOverview>;
  added: Record<Severity, SeverityOverview>;
};
