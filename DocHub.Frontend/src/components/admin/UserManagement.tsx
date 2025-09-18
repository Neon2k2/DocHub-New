import React, { useState, useEffect } from 'react';
import { User } from 'lucide-react';
import { 
  UserDto, 
  CreateUserRequest, 
  UpdateUserRequest, 
  RoleDto, 
  PermissionDto,
  PasswordValidationResult,
  apiService 
} from '../../services/api.service';
import { useTheme } from '../../contexts/ThemeContext';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '../ui/dialog';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';

interface UserManagementProps {
  onClose?: () => void;
}

const UserManagement: React.FC<UserManagementProps> = ({ onClose }) => {
  const { isDarkMode } = useTheme();
  const [users, setUsers] = useState<UserDto[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [passwordValidation, setPasswordValidation] = useState<PasswordValidationResult | null>(null);


  // Form states
  const [formData, setFormData] = useState<CreateUserRequest>({
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    department: 'ER',
    roleIds: []
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      console.log('🔄 [UserManagement] Starting to load data...');
      
      const [usersResponse, rolesResponse] = await Promise.all([
        apiService.getUsers({ page: 1, pageSize: 100 }),
        apiService.getRoles({ page: 1, pageSize: 100 })
      ]);

      console.log('🔍 [UserManagement] Users response:', usersResponse);
      console.log('🔍 [UserManagement] Users response.data:', usersResponse.data);
      console.log('🔍 [UserManagement] Users response.data.data:', usersResponse.data?.data);
      console.log('🔍 [UserManagement] Roles response:', rolesResponse);
      console.log('🔍 [UserManagement] Roles response.data:', rolesResponse.data);
      console.log('🔍 [UserManagement] Roles response.data.data:', rolesResponse.data?.data);

      if (usersResponse.success) {
        // Try different possible response structures
        const users = usersResponse.data?.data?.items || 
                     usersResponse.data?.items || 
                     usersResponse.data || 
                     [];
        console.log('🔍 [UserManagement] Extracted users:', users);
        console.log('🔍 [UserManagement] Users count:', users.length);
        console.log('🔍 [UserManagement] Users type:', typeof users, Array.isArray(users));
        setUsers(Array.isArray(users) ? users : []);
      } else {
        console.error('❌ [UserManagement] Users response failed:', usersResponse);
        setError(`Failed to load users: ${usersResponse.message || 'Unknown error'}`);
      }

      if (rolesResponse.success) {
        // Try different possible response structures
        const roles = rolesResponse.data?.data?.items || 
                     rolesResponse.data?.items || 
                     rolesResponse.data || 
                     [];
        console.log('🔍 [UserManagement] Extracted roles:', roles);
        console.log('🔍 [UserManagement] Roles count:', roles.length);
        console.log('🔍 [UserManagement] Roles type:', typeof roles, Array.isArray(roles));
        setRoles(Array.isArray(roles) ? roles : []);
      } else {
        console.error('❌ [UserManagement] Roles response failed:', rolesResponse);
        setError(`Failed to load roles: ${rolesResponse.message || 'Unknown error'}`);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error';
      setError(`Failed to load data: ${errorMessage}`);
      console.error('❌ [UserManagement] Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const response = await apiService.createUser(formData);
      if (response.success) {
        setUsers([...users, response.data!]);
        setShowCreateForm(false);
        resetForm();
      } else {
        setError('Failed to create user');
      }
    } catch (err) {
      setError('Failed to create user');
      console.error('Error creating user:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingUser) return;

    setLoading(true);
    try {
      const updateData: UpdateUserRequest = {
        username: formData.username,
        email: formData.email,
        firstName: formData.firstName,
        lastName: formData.lastName,
        department: formData.department,
        roleIds: formData.roleIds
      };

      const response = await apiService.updateUser(editingUser.id, updateData);
      if (response.success) {
        setUsers(users.map(u => u.id === editingUser.id ? response.data! : u));
        setEditingUser(null);
        resetForm();
      } else {
        setError('Failed to update user');
      }
    } catch (err) {
      setError('Failed to update user');
      console.error('Error updating user:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteUser = async (userId: string) => {
    if (!window.confirm('Are you sure you want to delete this user?')) return;

    setLoading(true);
    try {
      const response = await apiService.deleteUser(userId);
      if (response.success) {
        setUsers(users.filter(u => u.id !== userId));
      } else {
        setError('Failed to delete user');
      }
    } catch (err) {
      setError('Failed to delete user');
      console.error('Error deleting user:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleToggleUserStatus = async (userId: string) => {
    setLoading(true);
    try {
      const response = await apiService.toggleUserStatus(userId);
      if (response.success) {
        setUsers(users.map(u => 
          u.id === userId ? { ...u, isActive: !u.isActive } : u
        ));
      } else {
        setError('Failed to toggle user status');
      }
    } catch (err) {
      setError('Failed to toggle user status');
      console.error('Error toggling user status:', err);
    } finally {
      setLoading(false);
    }
  };

  const handlePasswordValidation = (password: string) => {
    if (!password) {
      setPasswordValidation(null);
      return;
    }

    const errors: string[] = [];
    
    if (password.length < 8) {
      errors.push('Password must be at least 8 characters long');
    }
    
    if (!/[A-Z]/.test(password)) {
      errors.push('Password must contain at least one uppercase letter');
    }
    
    if (!/[a-z]/.test(password)) {
      errors.push('Password must contain at least one lowercase letter');
    }
    
    if (!/\d/.test(password)) {
      errors.push('Password must contain at least one number');
    }
    
    if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
      errors.push('Password must contain at least one special character');
    }

    let strength: 'weak' | 'medium' | 'strong' = 'weak';
    if (errors.length === 0) {
      strength = password.length >= 12 ? 'strong' : 'medium';
    }

    setPasswordValidation({
      isValid: errors.length === 0,
      errors,
      strength
    });
  };

  const resetForm = () => {
    setFormData({
      username: '',
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      department: 'ER',
      roleIds: []
    });
    setPasswordValidation(null);
  };

  const startEdit = (user: UserDto) => {
    setEditingUser(user);
    setFormData({
      username: user.username,
      email: user.email,
      password: '',
      firstName: user.firstName,
      lastName: user.lastName,
      department: user.department,
      roleIds: user.roles.map(roleName => 
        roles.find(r => r.name === roleName)?.id || ''
      ).filter(Boolean)
    });
    setShowCreateForm(true);
  };

  const getPasswordStrengthColor = (strength: string) => {
    switch (strength) {
      case 'weak': return 'text-red-500';
      case 'medium': return 'text-yellow-500';
      case 'strong': return 'text-green-500';
      default: return 'text-gray-500';
    }
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
        <div>
          <h2 className={`text-2xl font-bold ${isDarkMode ? 'text-white' : 'text-gray-900'}`}>
            User Management
          </h2>
          <p className={`text-sm ${isDarkMode ? 'text-gray-400' : 'text-gray-600'} mt-1`}>
            {users.length} user{users.length !== 1 ? 's' : ''} found
          </p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={loadData}
            disabled={loading}
            className={`px-4 py-2 text-sm font-medium border rounded-md transition-colors ${
              isDarkMode 
                ? 'text-gray-300 bg-gray-700 border-gray-600 hover:bg-gray-600 disabled:opacity-50' 
                : 'text-gray-700 bg-gray-100 border-gray-300 hover:bg-gray-200 disabled:opacity-50'
            }`}
          >
            {loading ? 'Loading...' : 'Refresh'}
          </button>
          <button
            onClick={() => setShowCreateForm(true)}
            style={{
              minWidth: '120px',
              fontSize: '14px',
              fontWeight: '600',
              backgroundColor: '#2563eb',
              color: 'white',
              border: '2px solid #3b82f6',
              borderRadius: '8px',
              padding: '12px 24px',
              cursor: 'pointer',
              zIndex: 10,
              position: 'relative',
              display: 'block',
              visibility: 'visible',
              opacity: 1,
              boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
              transition: 'all 0.2s ease-in-out'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = '#1d4ed8';
              e.currentTarget.style.borderColor = '#2563eb';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = '#2563eb';
              e.currentTarget.style.borderColor = '#3b82f6';
            }}
          >
            + Add User
          </button>
          {onClose && (
            <button
              onClick={onClose}
              className={`px-4 py-2 rounded font-medium transition-colors ${
                isDarkMode 
                  ? 'bg-gray-700 text-white hover:bg-gray-600 border border-gray-600' 
                  : 'bg-gray-500 text-white hover:bg-gray-600'
              }`}
            >
              Close
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className={`px-4 py-3 rounded mb-4 ${
          isDarkMode 
            ? 'bg-red-900/20 border border-red-500 text-red-300' 
            : 'bg-red-100 border border-red-400 text-red-700'
        }`}>
          {error}
        </div>
      )}

      {/* User List */}
      <div className={`shadow rounded-lg overflow-hidden ${
        isDarkMode 
          ? 'bg-gray-800 border border-gray-700' 
          : 'bg-white'
      }`}>
        {users.length === 0 && !loading ? (
          <div className="text-center py-12">
            <div className={`text-lg font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-900'} mb-2`}>
              No users found
            </div>
            <div className={`text-sm ${isDarkMode ? 'text-gray-400' : 'text-gray-600'} mb-4`}>
              {error ? error : 'Try refreshing the data or add a new user.'}
            </div>
            <button
              onClick={loadData}
              className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
                isDarkMode 
                  ? 'bg-blue-600 hover:bg-blue-700 text-white' 
                  : 'bg-blue-600 hover:bg-blue-700 text-white'
              }`}
            >
              Refresh Data
            </button>
          </div>
        ) : (
          <table className={`min-w-full divide-y ${isDarkMode ? 'divide-gray-700' : 'divide-gray-200'}`}>
            <thead className={isDarkMode ? 'bg-gray-700' : 'bg-gray-50'}>
              <tr>
                <th className={`px-6 py-3 text-left text-xs font-medium uppercase tracking-wider ${
                  isDarkMode ? 'text-gray-300' : 'text-gray-500'
                }`}>
                  User
                </th>
                <th className={`px-6 py-3 text-left text-xs font-medium uppercase tracking-wider ${
                  isDarkMode ? 'text-gray-300' : 'text-gray-500'
                }`}>
                  Department
                </th>
                <th className={`px-6 py-3 text-left text-xs font-medium uppercase tracking-wider ${
                  isDarkMode ? 'text-gray-300' : 'text-gray-500'
                }`}>
                  Roles
                </th>
                <th className={`px-6 py-3 text-left text-xs font-medium uppercase tracking-wider ${
                  isDarkMode ? 'text-gray-300' : 'text-gray-500'
                }`}>
                  Status
                </th>
                <th className={`px-6 py-3 text-left text-xs font-medium uppercase tracking-wider ${
                  isDarkMode ? 'text-gray-300' : 'text-gray-500'
                }`}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className={`divide-y ${isDarkMode ? 'bg-gray-800 divide-gray-700' : 'bg-white divide-gray-200'}`}>
              {users.map((user) => (
              <tr key={user.id} className={isDarkMode ? 'hover:bg-gray-700' : 'hover:bg-gray-50'}>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div>
                    <div className={`text-sm font-medium ${isDarkMode ? 'text-white' : 'text-gray-900'}`}>
                      {user.firstName} {user.lastName}
                    </div>
                    <div className={`text-sm ${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}>
                      {user.email}
                    </div>
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                    isDarkMode 
                      ? 'bg-blue-900/30 text-blue-300 border border-blue-700' 
                      : 'bg-blue-100 text-blue-800'
                  }`}>
                    {user.department}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="flex flex-wrap gap-1">
                    {user.roles.map((role, index) => (
                      <span
                        key={index}
                        className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                          isDarkMode 
                            ? 'bg-green-900/30 text-green-300 border border-green-700' 
                            : 'bg-green-100 text-green-800'
                        }`}
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      user.isActive
                        ? isDarkMode 
                          ? 'bg-green-900/30 text-green-300 border border-green-700'
                          : 'bg-green-100 text-green-800'
                        : isDarkMode 
                          ? 'bg-red-900/30 text-red-300 border border-red-700'
                          : 'bg-red-100 text-red-800'
                    }`}
                  >
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  <div className="flex gap-2">
                    <button
                      onClick={() => startEdit(user)}
                      className={`transition-colors ${
                        isDarkMode 
                          ? 'text-blue-400 hover:text-blue-300' 
                          : 'text-blue-600 hover:text-blue-900'
                      }`}
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleToggleUserStatus(user.id)}
                      className={`transition-colors ${
                        isDarkMode 
                          ? 'text-yellow-400 hover:text-yellow-300' 
                          : 'text-yellow-600 hover:text-yellow-900'
                      }`}
                    >
                      {user.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                    <button
                      onClick={() => handleDeleteUser(user.id)}
                      className={`transition-colors ${
                        isDarkMode 
                          ? 'text-red-400 hover:text-red-300' 
                          : 'text-red-600 hover:text-red-900'
                      }`}
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Create/Edit Form Modal */}
      <Dialog open={showCreateForm} onOpenChange={setShowCreateForm}>
        <DialogContent className="dialog-panel max-w-md max-h-[90vh] overflow-y-auto">
          <DialogHeader className="flex-shrink-0">
            <DialogTitle className="flex items-center gap-2">
              <User className="h-5 w-5" />
              {editingUser ? 'Edit User' : 'Create User'}
            </DialogTitle>
            <DialogDescription>
              {editingUser ? 'Update user information and permissions' : 'Add a new user to the system'}
            </DialogDescription>
          </DialogHeader>

          <div className="flex-1 min-h-0 overflow-y-auto px-1">
            <form id="user-form" className="space-y-4 pb-4">
            <div className="space-y-2">
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                type="text"
                value={formData.username}
                onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="firstName">First Name</Label>
              <Input
                id="firstName"
                type="text"
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="lastName">Last Name</Label>
              <Input
                id="lastName"
                type="text"
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="department">Department</Label>
              <select
                id="department"
                value={formData.department}
                onChange={(e) => setFormData({ ...formData, department: e.target.value })}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                required
              >
                <option value="ER">ER</option>
                <option value="Billing">Billing</option>
              </select>
            </div>

            {!editingUser && (
              <div className="space-y-2">
                <Label htmlFor="password">Password</Label>
                <Input
                  id="password"
                  type="password"
                  value={formData.password}
                  onChange={(e) => {
                    setFormData({ ...formData, password: e.target.value });
                    handlePasswordValidation(e.target.value);
                  }}
                  required
                />
                {passwordValidation && (
                  <div className="space-y-1">
                    <div className={`text-sm ${getPasswordStrengthColor(passwordValidation.strength)}`}>
                      Password Strength: {passwordValidation.strength.toUpperCase()}
                    </div>
                    {passwordValidation.errors.length > 0 && (
                      <ul className="text-sm text-red-500">
                        {passwordValidation.errors.map((error, index) => (
                          <li key={index}>• {error}</li>
                        ))}
                      </ul>
                    )}
                  </div>
                )}
              </div>
            )}

            <div className="space-y-2">
              <Label>Roles</Label>
              <div className="space-y-2">
                {roles.map((role) => (
                  <label key={role.id} className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      checked={formData.roleIds.includes(role.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setFormData({
                            ...formData,
                            roleIds: [...formData.roleIds, role.id]
                          });
                        } else {
                          setFormData({
                            ...formData,
                            roleIds: formData.roleIds.filter(id => id !== role.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border border-input bg-background text-primary focus:ring-2 focus:ring-ring focus:ring-offset-2"
                    />
                    <span className="text-sm">{role.name}</span>
                  </label>
                ))}
              </div>
            </div>

            </form>
          </div>

          <div className="flex-shrink-0 flex justify-end space-x-2 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowCreateForm(false);
                setEditingUser(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              form="user-form"
              disabled={loading}
              onClick={(e) => {
                e.preventDefault();
                if (editingUser) {
                  handleUpdateUser(e);
                } else {
                  handleCreateUser(e);
                }
              }}
            >
              {loading ? 'Saving...' : editingUser ? 'Update' : 'Create'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default UserManagement;