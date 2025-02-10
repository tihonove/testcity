import React from 'react';
import { SeverityIcon } from '../SeverityIcon';
import { Issue } from '../types/Issue';
import { Severity } from '../types/Severity';
import { ApproximateUnits, ITEMS_UNITS } from './ApproximateUnits';
import { TreeView } from './TreeView';
import { TreeNode } from './types';
import styled from 'styled-components';

type Issues = Record<Severity, number>;

const initialIssues: Issues = {
  info: 0,
  minor: 0,
  major: 0,
  critical: 0,
  blocker: 0,
};

const DiffInfo = styled.div`
  display: flex;
  gap: 0.5rem;
  margin-left: 0.5rem;
  font-size: 0.75rem;
`;

interface IssuesTreeProps {
  issues: Issue[];
  prefix: string;
  onChangePrefix: (prefix: string) => void;
}

export function IssuesTree({ issues, prefix, onChangePrefix }: IssuesTreeProps) {
  return (
    <TreeView
      data={toTree(issues)}
      selectedPath={prefix}
      onSelect={(n) => onChangePrefix(n.path)}
      renderDetails={(details) => (
        <DiffInfo>
          {details.blocker !== 0 && <><SeverityIcon type='blocker'/><ApproximateUnits value={details.blocker} units={ITEMS_UNITS} /></>}{' '}
          {details.critical !== 0 && <><SeverityIcon type='critical'/><ApproximateUnits value={details.critical} units={ITEMS_UNITS} /></>}{' '}
          {details.major !== 0 && <><SeverityIcon type='major'/><ApproximateUnits value={details.major} units={ITEMS_UNITS} /></>}{' '}
          {details.minor !== 0 && <><SeverityIcon type='minor'/><ApproximateUnits value={details.minor} units={ITEMS_UNITS} /></>}{' '}
          {details.info !== 0 && <><SeverityIcon type='info'/><ApproximateUnits value={details.info} units={ITEMS_UNITS} /></>}{' '}
        </DiffInfo>
      )}
    />
  );
}

function toTree(issues: Issue[]): TreeNode<Issues>[] {
  const root: TreeNode<Issues>[] = [];

  issues.forEach((issue) => {
    const path = issue.location.path;
    const parts = path.split('/');
    let currentNodes = root;

    for (let i = 0; i < parts.length; i++) {
      const part = parts[i];
      const isFile = i === parts.length - 1;
      const type = isFile ? 'file' : 'directory';
      const existingNode = currentNodes.find((n) => n.name === part && n.type === type);

      if (!existingNode) {
        const newNode: TreeNode<Issues> = {
          id: parts.slice(0, i + 1).join('/'),
          name: part,
          type,
          path: parts.slice(0, i + 1).join('/'),
          children: type === 'directory' ? [] : undefined,
          details: { ...initialIssues },
        };
        newNode.details![issue.severity] += 1;
        currentNodes.push(newNode);
        currentNodes = newNode.children!;
      } else {
        existingNode.details![issue.severity] += 1;
        currentNodes = existingNode.children!;
      }
    }
  });

  return mergeSingleChildDirectories(root);
}

function mergeSingleChildDirectories(nodes: TreeNode<Issues>[]): TreeNode<Issues>[] {
  for (let i = 0; i < nodes.length; i++) {
    nodes.sort(sortBySeverity);

    const node = nodes[i];
    if (node.type === 'directory' && node.children?.length === 1) {
      const child = node.children[0];
      if (child.type === 'directory') {
        // Merge node with its single directory child
        node.name += '/' + child.name;
        node.path = child.path;
        node.id = child.id;
        node.children = child.children;
        // Re-check this node as it might now have a single child
        i--;
      }
    } else if (node.type === 'directory' && node.children) {
      mergeSingleChildDirectories(node.children);
    }
  }
  return nodes;
}

function sortByName<T>(a: TreeNode<T>, b: TreeNode<T>): number {
    if (a.type === 'directory' && b.type === 'file') {
      return -1;
    }
    if (a.type === 'file' && b.type === 'directory') {
      return 1;
    }

    return a.name.localeCompare(b.name);
}

function sortBySeverity(a: TreeNode<Issues>, b: TreeNode<Issues>): number {
  const severityOrder: Severity[] = ['blocker', 'critical', 'major', 'minor', 'info'];
  for (const severity of severityOrder) {
    const severityA = a.details![severity];
    const severityB = b.details![severity];
    if (severityA === 0 && severityB === 0) {
      continue;
    }
    if (severityA !== 0 && severityB !== 0) {
      return severityB - severityA;
    }
    if (severityA === 0) {
      return 1;
    }
    if (severityB === 0) {
      return -1;
    }
  }

  return sortByName(a, b);
}