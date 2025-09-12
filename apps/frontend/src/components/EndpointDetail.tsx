'use client';

import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import React from 'react';

import { type AuthProfileDto } from '@/lib/api/auth-profiles';
import { type EndpointDto } from '@/lib/api/endpoints';

interface Props {
  open: boolean;
  onClose: () => void;
  selectedEndpoint: EndpointDto | null;
  detailLoading: boolean;
  authProfiles: AuthProfileDto[];
  selectedAuthProfile: string;
  setSelectedAuthProfile: (v: string) => void;
  testGenerating: boolean;
  onGenerateTests: (mode?: 'CARD' | 'FILES') => void;
}

const EndpointDetail: React.FC<Props> = ({
  open,
  onClose,
  selectedEndpoint,
  detailLoading,
  authProfiles,
  selectedAuthProfile,
  setSelectedAuthProfile,
  testGenerating,
  onGenerateTests,
}) => {
  // Type guards
  const isRequestBody = (
    request: unknown
  ): request is {
    contentType?: string;
    description?: string;
    required?: boolean;
  } => {
    return typeof request === 'object' && request !== null;
  };

  const isResponseMap = (
    responses: unknown
  ): responses is Record<
    string,
    { description?: string; contentType?: string }
  > => {
    return typeof responses === 'object' && responses !== null;
  };

  const isSecurityArray = (
    security: unknown
  ): security is Array<Record<string, string[]>> => {
    return Array.isArray(security);
  };

  const isParameterArray = (
    parameters: unknown
  ): parameters is Array<unknown> => {
    return Array.isArray(parameters);
  };

  return (
    <Dialog
      open={open}
      onOpenChange={() => {
        onClose();
      }}
    >
      <DialogContent className="max-w-4xl max-h-[80vh] overflow-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="font-mono text-sm bg-blue-100 dark:bg-blue-900/50 px-2 py-1 rounded text-blue-800 dark:text-blue-200">
              {selectedEndpoint?.method?.toUpperCase()}
            </span>
            <span className="font-mono">{selectedEndpoint?.path}</span>
          </DialogTitle>
        </DialogHeader>

        {detailLoading ? (
          <div className="flex items-center justify-center py-8">
            <div>Loading endpoint details...</div>
          </div>
        ) : selectedEndpoint ? (
          <div className="space-y-6">
            <div>
              <h3 className="font-semibold mb-2">Summary</h3>
              <p className="text-sm text-muted-foreground">
                {selectedEndpoint.summary || 'No summary available'}
              </p>
            </div>

            {selectedEndpoint.description && (
              <div>
                <h3 className="font-semibold mb-2">Description</h3>
                <p className="text-sm text-muted-foreground whitespace-pre-wrap">
                  {selectedEndpoint.description}
                </p>
              </div>
            )}

            {selectedEndpoint.operationId && (
              <div>
                <h3 className="font-semibold mb-2">Operation ID</h3>
                <code className="text-sm bg-muted px-2 py-1 rounded border">
                  {selectedEndpoint.operationId}
                </code>
              </div>
            )}

            {selectedEndpoint.tags && selectedEndpoint.tags.length > 0 && (
              <div>
                <h3 className="font-semibold mb-2">Tags</h3>
                <div className="flex flex-wrap gap-2">
                  {selectedEndpoint.tags.map((tag, index) => (
                    <span
                      key={index}
                      className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {Array.isArray(selectedEndpoint.servers) &&
              selectedEndpoint.servers.length > 0 && (
                <div>
                  <h3 className="font-semibold mb-2">Servers</h3>
                  <div className="space-y-1">
                    {selectedEndpoint.servers.map((server, index) => (
                      <div
                        key={index}
                        className="text-sm font-mono bg-gray-100 px-2 py-1 rounded"
                      >
                        {server}
                      </div>
                    ))}
                  </div>
                </div>
              )}

            {isParameterArray(selectedEndpoint.parameters) &&
              selectedEndpoint.parameters.length > 0 && (
                <div>
                  <h3 className="font-semibold mb-2">Parameters</h3>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm border-collapse border border-border">
                      <thead>
                        <tr className="bg-muted/50">
                          <th className="border border-border px-3 py-2 text-left">
                            Name
                          </th>
                          <th className="border border-border px-3 py-2 text-left">
                            In
                          </th>
                          <th className="border border-border px-3 py-2 text-left">
                            Type
                          </th>
                          <th className="border border-border px-3 py-2 text-left">
                            Required
                          </th>
                          <th className="border border-border px-3 py-2 text-left">
                            Description
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {selectedEndpoint.parameters.map(
                          (param: unknown, index: number) => {
                            const p = param as Record<string, unknown>;
                            return (
                              <tr key={index} className="hover:bg-muted/30">
                                <td className="border border-border px-3 py-2 font-mono">
                                  {(p.name as string) ||
                                    (p.Name as string) ||
                                    'N/A'}
                                </td>
                                <td className="border border-border px-3 py-2">
                                  {(p.in as string) ||
                                    (p.In as string) ||
                                    'N/A'}
                                </td>
                                <td className="border border-border px-3 py-2">
                                  {((p.schema as Record<string, unknown>)
                                    ?.type as string) ||
                                    ((p.Schema as Record<string, unknown>)
                                      ?.Type as string) ||
                                    (p.type as string) ||
                                    'string'}
                                </td>
                                <td className="border border-border px-3 py-2">
                                  {((p.required ?? p.Required) as boolean)
                                    ? 'Yes'
                                    : 'No'}
                                </td>
                                <td className="border border-border px-3 py-2">
                                  {(p.description as string) ||
                                    (p.Description as string) ||
                                    '-'}
                                </td>
                              </tr>
                            );
                          }
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

            {isRequestBody(selectedEndpoint.request) && (
              <div>
                <h3 className="font-semibold mb-2">Request Body</h3>
                <div className="bg-muted/50 p-3 rounded text-sm border border-border">
                  {selectedEndpoint.request.required && (
                    <div className="text-destructive text-xs mb-2">
                      Required
                    </div>
                  )}
                  {selectedEndpoint.request.content &&
                    Object.keys(selectedEndpoint.request.content).length >
                      0 && (
                      <div>
                        <div className="font-medium mb-2">Content Types:</div>
                        <div className="text-muted-foreground">
                          {Object.keys(selectedEndpoint.request.content).join(
                            ', '
                          )}
                        </div>
                      </div>
                    )}
                </div>
              </div>
            )}

            {isResponseMap(selectedEndpoint.responses) &&
              Object.keys(selectedEndpoint.responses).length > 0 && (
                <div>
                  <h3 className="font-semibold mb-2">Responses</h3>
                  <div className="space-y-3">
                    {Object.entries(selectedEndpoint.responses).map(
                      ([status, response]) => (
                        <div
                          key={status}
                          className="border border-border rounded p-3 bg-card"
                        >
                          <div className="flex items-center gap-2 mb-2">
                            <span
                              className={`font-mono text-sm px-2 py-1 rounded ${
                                status.startsWith('2')
                                  ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                                  : status.startsWith('4')
                                  ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
                                  : status.startsWith('5')
                                  ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
                                  : 'bg-muted text-muted-foreground'
                              }`}
                            >
                              {status}
                            </span>
                            <span className="text-sm text-muted-foreground">
                              {response.description || 'No description'}
                            </span>
                          </div>
                          {response.content &&
                            Object.keys(response.content).length > 0 && (
                              <div className="text-xs text-muted-foreground">
                                Content-Types:{' '}
                                {Object.keys(response.content).join(', ')}
                              </div>
                            )}
                        </div>
                      )
                    )}
                  </div>
                </div>
              )}

            {isSecurityArray(selectedEndpoint.security) &&
              selectedEndpoint.security.length > 0 && (
                <div>
                  <h3 className="font-semibold mb-2">Security</h3>
                  <div className="space-y-2">
                    {selectedEndpoint.security.map(
                      (securityRequirement, index) => (
                        <div
                          key={index}
                          className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded p-2"
                        >
                          <div className="text-sm">
                            {Object.entries(securityRequirement).map(
                              ([name, scopes]) => (
                                <div key={name}>
                                  <span className="font-medium">{name}</span>
                                  {Array.isArray(scopes) &&
                                    scopes.length > 0 && (
                                      <span className="text-muted-foreground">
                                        : {scopes.join(', ')}
                                      </span>
                                    )}
                                </div>
                              )
                            )}
                          </div>
                        </div>
                      )
                    )}
                  </div>
                </div>
              )}

            <div className="border-t pt-6">
              <h3 className="font-semibold mb-4">Generate API Tests</h3>
              <div className="space-y-4">
                {selectedEndpoint.security &&
                  selectedEndpoint.security.length > 0 && (
                    <div>
                      <label className="text-sm font-medium mb-2 block">
                        Auth Profile
                      </label>
                      <Select
                        value={selectedAuthProfile}
                        onValueChange={setSelectedAuthProfile}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select an auth profile" />
                        </SelectTrigger>
                        <SelectContent>
                          {authProfiles.map(profile => (
                            <SelectItem key={profile.id} value={profile.id}>
                              {profile.type} -{' '}
                              {profile.injectionName || 'Default'}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  )}

                <div className="flex gap-2">
                  <Button
                    onClick={() => onGenerateTests('FILES')}
                    disabled={testGenerating}
                    className="flex-1"
                  >
                    {testGenerating ? 'Generating...' : 'Generate Tests'}
                  </Button>
                </div>

                {selectedEndpoint.security &&
                  selectedEndpoint.security.length > 0 &&
                  authProfiles.length === 0 && (
                    <p className="text-sm text-muted-foreground">
                      No auth profiles found. Create an auth profile in the
                      project settings to generate tests.
                    </p>
                  )}
              </div>
            </div>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  );
};

export default EndpointDetail;
