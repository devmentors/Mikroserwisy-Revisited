"use client"

import { Area, AreaChart, ResponsiveContainer, XAxis, YAxis } from 'recharts'

interface HistoryPoint {
  timestamp: string;
  value: number;
}

interface MiniSparklineProps {
  data: HistoryPoint[];
  color: string;
  width: number;
  height: number;
  maxValue?: number;
}

export function MiniSparkline({ data, color, width, height, maxValue }: MiniSparklineProps) {
  const chartData = data
    .slice(-10)
    .map(point => ({
      time: new Date(point.timestamp).getTime(),
      value: maxValue ? (point.value / maxValue) * 100 : point.value
    }))
    .sort((a, b) => a.time - b.time);

  if (!chartData.length) return null;

  return (
    <div style={{ width, height }}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={chartData} margin={{ top: 2, right: 0, bottom: 2, left: 0 }}>
          <XAxis 
            dataKey="time" 
            hide={true}
          />
          <YAxis 
            domain={[0, 100]} 
            hide={true}
          />
          <Area
            type="monotone"
            dataKey="value"
            stroke={color}
            fill={color}
            fillOpacity={0.1}
            strokeWidth={1.5}
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
} 