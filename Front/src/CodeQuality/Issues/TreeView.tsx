import * as React from "react";
import { useState, useCallback } from "react";
import { ArrowCRightIcon16Regular } from "@skbkontur/icons/ArrowCRightIcon16Regular";
import { ArrowCDownIcon16Regular } from "@skbkontur/icons/ArrowCDownIcon16Regular";
import { FolderIcon16Regular } from "@skbkontur/icons/FolderIcon16Regular";
import { FolderMinusIcon16Regular } from "@skbkontur/icons/FolderMinusIcon16Regular";
import { FileTypeMarkupIcon16Regular } from "@skbkontur/icons/FileTypeMarkupIcon16Regular";
import { TreeNode, TreeViewProps } from "./types";
import styled from "styled-components";
import { theme } from "../../Theme/ITheme";

export function TreeView<T>({ data, onSelect, selectedPath, renderDetails }: TreeViewProps<T>) {
    const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());

    const toggleNode = useCallback((path: string) => {
        setExpandedNodes(prev => {
            const next = new Set(prev);
            if (next.has(path)) {
                next.delete(path);
            } else {
                next.add(path);
            }
            return next;
        });
    }, []);

    const renderNode = (node: TreeNode<T>, level: number = 0) => {
        const isExpanded = expandedNodes.has(node.path);
        const isSelected = node.path === selectedPath;

        return (
            <div key={node.path}>
                <NodeContent
                    $selected={isSelected}
                    onClick={() => {
                        if (node.type === "directory") {
                            toggleNode(node.path);
                        }
                        onSelect?.(node);
                    }}
                    style={{ paddingLeft: level * 16 }}>
                    <IconWrapper>
                        {node.type === "directory" &&
                            (isExpanded ? <ArrowCDownIcon16Regular /> : <ArrowCRightIcon16Regular />)}
                    </IconWrapper>
                    <FileIconWrapper>
                        {node.type === "directory" ? (
                            isExpanded ? (
                                <FolderMinusIcon16Regular />
                            ) : (
                                <FolderIcon16Regular />
                            )
                        ) : (
                            <FileTypeMarkupIcon16Regular />
                        )}
                    </FileIconWrapper>
                    <NodeName title={node.name}>{node.name}</NodeName>
                    {node.details && renderDetails && renderDetails(node.details)}
                </NodeContent>
                {node.type === "directory" && isExpanded && node.children && (
                    <div role="group">{node.children.map(child => renderNode(child, level + 1))}</div>
                )}
            </div>
        );
    };

    return <TreeContainer role="tree">{data.map(node => renderNode(node))}</TreeContainer>;
}

const TreeContainer = styled.div`
    min-width: 400px;
    max-width: 400px;
    width: 400px;
    height: calc(100vh - 40px);
    width: 100%;
    overflow: auto;
`;

const NodeContent = styled.div<{ $selected: boolean }>`
    display: flex;
    align-items: center;
    padding: 0.25rem 0.5rem;
    cursor: pointer;
    user-select: none;
    background-color: ${props => (props.$selected ? props.theme.inverseColor("0.1") : "transparent")};
    color: ${theme.primaryTextColor};

    &:hover {
        background-color: ${props =>
            props.$selected ? props.theme.inverseColor("0.2") : props.theme.inverseColor("0.05")};
    }
`;

const IconWrapper = styled.div`
    width: 1rem;
    height: 1rem;
    margin-right: 0.25rem;
    flex-shrink: 0;
`;

const FileIconWrapper = styled.div`
    width: 1.25rem;
    height: 1.25rem;
    margin-right: 0.5rem;
    flex-shrink: 0;
    color: #6b7280;
`;

const NodeName = styled.span`
    font-size: 0.875rem;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    flex-grow: 1;
`;
