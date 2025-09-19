import { AnimatedContainer } from '@/components/auth-pilot/AnimationEffects';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { LogEntry } from '@/types/auth-pilot';

interface AuthPilotLogsProps {
  logs: LogEntry[];
}

export const AuthPilotLogs = ({ logs }: AuthPilotLogsProps) => {
  if (logs.length === 0) return null;

  return (
    <AnimatedContainer delay={1100}>
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Activity Log</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-2 max-h-32 overflow-y-auto">
            {logs.slice(-5).map((log, index) => (
              <div key={index} className="flex items-center gap-2 text-sm">
                <span className="text-slate-500 font-mono text-xs w-16">
                  {log.timestamp}
                </span>
                <Badge
                  variant={log.status === 'success' ? 'default' : 'destructive'}
                  className="text-xs"
                >
                  {log.type}
                </Badge>
                <span className="text-slate-700">{log.message}</span>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </AnimatedContainer>
  );
};
