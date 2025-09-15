import React, { useState, useEffect } from 'react';
import { 
  UserSessionDto, 
  UserDto,
  apiService 
} from '../../services/api.service';

interface SessionManagementProps {
  onClose?: () => void;
}

const SessionManagement: React.FC<SessionManagementProps> = ({ onClose }) => {
  const [sessions, setSessions] = useState<UserSessionDto[]>([]);
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedUserId, setSelectedUserId] = useState<string>('');

  useEffect(() => {
    loadUsers();
  }, []);

  useEffect(() => {
    if (selectedUserId) {
      loadUserSessions(selectedUserId);
    }
  }, [selectedUserId]);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const response = await apiService.getUsers();
      if (response.success) {
        setUsers(response.data || []);
        if (response.data && response.data.length > 0) {
          setSelectedUserId(response.data[0].id);
        }
      }
    } catch (err) {
      setError('Failed to load users');
      console.error('Error loading users:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadUserSessions = async (userId: string) => {
    setLoading(true);
    try {
      const response = await apiService.getUserSessions(userId);
      if (response.success) {
        setSessions(response.data || []);
      }
    } catch (err) {
      setError('Failed to load sessions');
      console.error('Error loading sessions:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleTerminateSession = async (sessionId: string) => {
    if (!window.confirm('Are you sure you want to terminate this session?')) return;

    setLoading(true);
    try {
      const response = await apiService.terminateSession(sessionId);
      if (response.success) {
        setSessions(sessions.filter(s => s.id !== sessionId));
      } else {
        setError('Failed to terminate session');
      }
    } catch (err) {
      setError('Failed to terminate session');
      console.error('Error terminating session:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleTerminateAllSessions = async (userId: string) => {
    if (!window.confirm('Are you sure you want to terminate all sessions for this user?')) return;

    setLoading(true);
    try {
      const response = await apiService.terminateAllUserSessions(userId);
      if (response.success) {
        setSessions([]);
      } else {
        setError('Failed to terminate all sessions');
      }
    } catch (err) {
      setError('Failed to terminate all sessions');
      console.error('Error terminating all sessions:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const getSessionStatusColor = (isActive: boolean, expiresAt: string) => {
    const now = new Date();
    const expiry = new Date(expiresAt);
    
    if (!isActive) return 'bg-red-100 text-red-800';
    if (expiry < now) return 'bg-yellow-100 text-yellow-800';
    return 'bg-green-100 text-green-800';
  };

  const getSessionStatusText = (isActive: boolean, expiresAt: string) => {
    const now = new Date();
    const expiry = new Date(expiresAt);
    
    if (!isActive) return 'Terminated';
    if (expiry < now) return 'Expired';
    return 'Active';
  };

  const getTimeAgo = (dateString: string) => {
    const now = new Date();
    const date = new Date(dateString);
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));
    
    if (diffInMinutes < 1) return 'Just now';
    if (diffInMinutes < 60) return `${diffInMinutes}m ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)}h ago`;
    return `${Math.floor(diffInMinutes / 1440)}d ago`;
  };

  if (loading && users.length === 0) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Session Management</h2>
        {onClose && (
          <button
            onClick={onClose}
            className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"
          >
            Close
          </button>
        )}
      </div>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {/* User Selection */}
      <div className="mb-6">
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Select User
        </label>
        <select
          value={selectedUserId}
          onChange={(e) => setSelectedUserId(e.target.value)}
          className="block w-full border border-gray-300 rounded-md px-3 py-2 bg-white"
        >
          {users.map((user) => (
            <option key={user.id} value={user.id}>
              {user.firstName} {user.lastName} ({user.email})
            </option>
          ))}
        </select>
      </div>

      {/* Sessions List */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex justify-between items-center">
            <h3 className="text-lg font-medium text-gray-900">
              Active Sessions ({sessions.filter(s => s.isActive).length})
            </h3>
            {sessions.length > 0 && (
              <button
                onClick={() => handleTerminateAllSessions(selectedUserId)}
                className="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600"
              >
                Terminate All Sessions
              </button>
            )}
          </div>
        </div>

        {sessions.length === 0 ? (
          <div className="px-6 py-4 text-center text-gray-500">
            No sessions found for this user.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Login Time
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Last Activity
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Expires At
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    IP Address
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    User Agent
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {sessions.map((session) => (
                  <tr key={session.id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span
                        className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getSessionStatusColor(
                          session.isActive,
                          session.expiresAt
                        )}`}
                      >
                        {getSessionStatusText(session.isActive, session.expiresAt)}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      <div>{formatDate(session.loginTime)}</div>
                      <div className="text-xs text-gray-500">
                        {getTimeAgo(session.loginTime)}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      <div>{formatDate(session.lastActivityAt)}</div>
                      <div className="text-xs text-gray-500">
                        {getTimeAgo(session.lastActivityAt)}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatDate(session.expiresAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {session.ipAddress || 'N/A'}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900 max-w-xs truncate">
                      {session.userAgent || 'N/A'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      {session.isActive && (
                        <button
                          onClick={() => handleTerminateSession(session.id)}
                          className="text-red-600 hover:text-red-900"
                        >
                          Terminate
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Session Statistics */}
      {sessions.length > 0 && (
        <div className="mt-6 grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-white p-4 rounded-lg shadow">
            <div className="text-sm font-medium text-gray-500">Total Sessions</div>
            <div className="text-2xl font-bold text-gray-900">{sessions.length}</div>
          </div>
          <div className="bg-white p-4 rounded-lg shadow">
            <div className="text-sm font-medium text-gray-500">Active Sessions</div>
            <div className="text-2xl font-bold text-green-600">
              {sessions.filter(s => s.isActive).length}
            </div>
          </div>
          <div className="bg-white p-4 rounded-lg shadow">
            <div className="text-sm font-medium text-gray-500">Expired Sessions</div>
            <div className="text-2xl font-bold text-red-600">
              {sessions.filter(s => !s.isActive || new Date(s.expiresAt) < new Date()).length}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SessionManagement;
