"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.WorkflowVisualizer = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
const WorkflowVisualizer = ({ layout, activeNodeName, completedNodeNames = [], failedNodeNames = [], nodeStates, completedConnectionIds = [], }) => {
    const [transform, setTransform] = (0, react_1.useState)({ x: 0, y: 0, zoom: 1 });
    const [isDragging, setIsDragging] = (0, react_1.useState)(false);
    const dragStart = (0, react_1.useRef)({ x: 0, y: 0 });
    const svgRef = (0, react_1.useRef)(null);
    // Compute total dimensions to size the default viewBox
    const nodes = layout?.nodes || [];
    const connections = layout?.connections || [];
    const maxX = nodes.reduce((max, n) => Math.max(max, (n.x ?? 0) + (n.width ?? 180)), 800);
    const maxY = nodes.reduce((max, n) => Math.max(max, (n.y ?? 0) + (n.height ?? 60)), 600);
    const canvasWidth = maxX + 100;
    const canvasHeight = maxY + 100;
    // Zoom & Pan Handlers
    const handleMouseDown = (e) => {
        // Only drag with left click
        if (e.button !== 0)
            return;
        setIsDragging(true);
        dragStart.current = { x: e.clientX - transform.x, y: e.clientY - transform.y };
    };
    const handleMouseMove = (e) => {
        if (!isDragging)
            return;
        setTransform({
            ...transform,
            x: e.clientX - dragStart.current.x,
            y: e.clientY - dragStart.current.y,
        });
    };
    const handleMouseUpOrLeave = () => {
        setIsDragging(false);
    };
    const handleWheel = (e) => {
        e.preventDefault();
        const zoomFactor = 1.1;
        let nextZoom = e.deltaY < 0 ? transform.zoom * zoomFactor : transform.zoom / zoomFactor;
        // Limit zoom level bounds
        nextZoom = Math.max(0.3, Math.min(2.5, nextZoom));
        setTransform({
            ...transform,
            zoom: nextZoom,
        });
    };
    const resetTransform = () => {
        setTransform({ x: 0, y: 0, zoom: 1 });
    };
    return ((0, jsx_runtime_1.jsxs)("div", { className: "arora-visualizer-container", style: { position: 'relative', width: '100%', height: '100%', minHeight: '350px', background: '#0f172a', borderRadius: '12px', overflow: 'hidden', border: '1px solid #334155' }, children: [(0, jsx_runtime_1.jsx)("button", { onClick: resetTransform, style: {
                    position: 'absolute',
                    top: '12px',
                    right: '12px',
                    background: 'rgba(30, 41, 59, 0.8)',
                    border: '1px solid #475569',
                    color: '#e2e8f0',
                    padding: '6px 12px',
                    borderRadius: '6px',
                    fontSize: '12px',
                    cursor: 'pointer',
                    zIndex: 10,
                    backdropFilter: 'blur(4px)',
                    transition: 'all 0.2s',
                }, onMouseEnter: (e) => (e.currentTarget.style.background = '#334155'), onMouseLeave: (e) => (e.currentTarget.style.background = 'rgba(30, 41, 59, 0.8)'), children: "Reset View" }), (0, jsx_runtime_1.jsxs)("svg", { ref: svgRef, width: "100%", height: "100%", viewBox: `0 0 ${canvasWidth} ${canvasHeight}`, onMouseDown: handleMouseDown, onMouseMove: handleMouseMove, onMouseUp: handleMouseUpOrLeave, onMouseLeave: handleMouseUpOrLeave, onWheel: handleWheel, style: { cursor: isDragging ? 'grabbing' : 'grab', userSelect: 'none' }, children: [(0, jsx_runtime_1.jsxs)("defs", { children: [(0, jsx_runtime_1.jsx)("marker", { id: "arrow", viewBox: "0 0 10 10", refX: "6", refY: "5", markerWidth: "6", markerHeight: "6", orient: "auto-start-reverse", children: (0, jsx_runtime_1.jsx)("path", { d: "M 0 0 L 10 5 L 0 10 z", fill: "#64748b" }) }), (0, jsx_runtime_1.jsx)("marker", { id: "arrow-active", viewBox: "0 0 10 10", refX: "6", refY: "5", markerWidth: "6", markerHeight: "6", orient: "auto-start-reverse", children: (0, jsx_runtime_1.jsx)("path", { d: "M 0 0 L 10 5 L 0 10 z", fill: "#3b82f6" }) })] }), (0, jsx_runtime_1.jsxs)("g", { transform: `translate(${transform.x}, ${transform.y}) scale(${transform.zoom})`, children: [connections.map((conn, idx) => {
                                const fromNode = conn.fromNode || '';
                                const toNode = conn.toNode || '';
                                if (!fromNode || !toNode)
                                    return null;
                                const isFromActive = activeNodeName && fromNode.toLowerCase() === activeNodeName.toLowerCase();
                                const isCompleted = completedNodeNames.some((n) => n.toLowerCase() === fromNode.toLowerCase() &&
                                    completedNodeNames.some((c) => c.toLowerCase() === toNode.toLowerCase()));
                                const isCompletedConnection = completedConnectionIds?.includes(`${fromNode.toLowerCase()}->${toNode.toLowerCase()}`) ?? false;
                                let strokeColor = '#475569'; // Default line color
                                let strokeWidth = '2';
                                let markerId = 'arrow';
                                if (isCompletedConnection || isCompleted) {
                                    strokeColor = '#10b981'; // Green completed transitions
                                    strokeWidth = '2.5';
                                }
                                else if (isFromActive) {
                                    strokeColor = '#3b82f6'; // Blue active node transitions
                                    strokeWidth = '2.5';
                                    markerId = 'arrow-active';
                                }
                                // Create SVG path string from points list
                                const points = conn.points || [];
                                let pathData = '';
                                if (points.length > 0) {
                                    pathData = `M ${points[0].x ?? 0} ${points[0].y ?? 0}`;
                                    for (let i = 1; i < points.length; i++) {
                                        pathData += ` L ${points[i].x ?? 0} ${points[i].y ?? 0}`;
                                    }
                                }
                                // Calculate label position in center of path
                                let labelX = 0;
                                let labelY = 0;
                                if (points.length >= 2) {
                                    const midIdx = Math.floor(points.length / 2);
                                    const midPoint = points[midIdx];
                                    if (midPoint) {
                                        labelX = midPoint.x ?? 0;
                                        labelY = (midPoint.y ?? 0) - 8;
                                    }
                                }
                                return ((0, jsx_runtime_1.jsxs)("g", { children: [(0, jsx_runtime_1.jsx)("path", { d: pathData, fill: "none", stroke: strokeColor, strokeWidth: strokeWidth, markerEnd: `url(#${markerId})`, style: { transition: 'stroke 0.3s, stroke-width 0.3s' } }), conn.condition && ((0, jsx_runtime_1.jsx)("text", { x: labelX, y: labelY, fill: isCompleted ? '#34d399' : '#94a3b8', fontSize: "10", fontWeight: "600", textAnchor: "middle", style: {
                                                background: '#0f172a',
                                                padding: '2px',
                                            }, children: conn.condition }))] }, `conn-${idx}`));
                            }), nodes.map((node) => {
                                const name = node.name || '';
                                if (!name)
                                    return null;
                                const state = nodeStates ? nodeStates[name] : undefined;
                                const isCurrent = state ? state === 'active' : (activeNodeName && name.toLowerCase() === activeNodeName.toLowerCase());
                                const isCompleted = state ? state === 'completed' : completedNodeNames.some((n) => n.toLowerCase() === name.toLowerCase());
                                const isFailed = state ? state === 'failed' : failedNodeNames.some((n) => n.toLowerCase() === name.toLowerCase());
                                const isCancelled = state ? state === 'cancelled' : false;
                                const isBypassed = state ? state === 'bypassed' : false;
                                const x = node.x ?? 0;
                                const y = node.y ?? 0;
                                const width = node.width ?? 180;
                                const height = node.height ?? 60;
                                const cx = x + (width / 2);
                                const cy = y + (height / 2);
                                // Styling variables
                                let fill = '#1e293b';
                                let stroke = '#475569';
                                let strokeWidth = '1.5';
                                let textColor = '#cbd5e1';
                                let className = '';
                                let opacity = '1';
                                if (isCurrent) {
                                    fill = '#1e3a8a';
                                    stroke = '#3b82f6';
                                    strokeWidth = '2.5';
                                    textColor = '#eff6ff';
                                    className = 'arora-node-pulse';
                                }
                                else if (isFailed) {
                                    fill = '#450a0a';
                                    stroke = '#ef4444';
                                    textColor = '#fef2f2';
                                }
                                else if (isCompleted) {
                                    fill = '#064e3b';
                                    stroke = '#10b981';
                                    textColor = '#ecfdf5';
                                }
                                else if (isCancelled) {
                                    fill = '#2e2a24';
                                    stroke = '#f97316';
                                    textColor = '#ffedd5';
                                }
                                else if (isBypassed) {
                                    fill = '#0f172a';
                                    stroke = '#334155';
                                    textColor = '#64748b';
                                    opacity = '0.4';
                                }
                                const type = node.type || 'Step';
                                const isApproval = type.toLowerCase() === 'approval';
                                return ((0, jsx_runtime_1.jsxs)("g", { className: className, style: { transition: 'all 0.3s', opacity }, children: [isApproval ? (
                                        // Diamond shape for approvals
                                        (0, jsx_runtime_1.jsx)("polygon", { points: `${cx},${y} ${x + width},${cy} ${cx},${y + height} ${x},${cy}`, fill: fill, stroke: stroke, strokeWidth: strokeWidth, style: { filter: isCurrent ? 'drop-shadow(0 0 8px rgba(59, 130, 246, 0.5))' : 'none' } })) : (
                                        // Rectangle with rounded corners for steps
                                        (0, jsx_runtime_1.jsx)("rect", { x: x, y: y, width: width, height: height, rx: "8", fill: fill, stroke: stroke, strokeWidth: strokeWidth, style: { filter: isCurrent ? 'drop-shadow(0 0 8px rgba(59, 130, 246, 0.5))' : 'none' } })), (0, jsx_runtime_1.jsx)("text", { x: cx, y: cy, fill: textColor, fontSize: "11", fontWeight: "600", textAnchor: "middle", dominantBaseline: "central", pointerEvents: "none", children: name }), isCurrent && ((0, jsx_runtime_1.jsx)("circle", { cx: x + width - 6, cy: y + 6, r: "4", fill: "#3b82f6", className: "arora-ping-dot" })), isCompleted && !isCurrent && ((0, jsx_runtime_1.jsx)("circle", { cx: x + width - 6, cy: y + 6, r: "4", fill: "#10b981" }))] }, `node-${name}`));
                            })] })] })] }));
};
exports.WorkflowVisualizer = WorkflowVisualizer;
