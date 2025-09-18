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
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
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
        apiService.getUsers({ page: 1, pageSize: 100, isActive: true }),
        apiService.getRoles({ page: 1, pageSize: 100 })
      ]);

      console.log('🔍 [UserManagement] Users response:', usersResponse);
      console.log('🔍 [UserManagement] Users response.data:', usersResponse.data);
      console.log('🔍 [UserManagement] Roles response:', rolesResponse);
      console.log('🔍 [UserManagement] Roles response.data:', rolesResponse.data);

      if (usersResponse.success) {
        // Try different possible response structures
        const users = usersResponse.data?.items || [];
        console.log('🔍 [UserManagement] Extracted users:', users);
        console.log('🔍 [UserManagement] Users count:', users.length);
        console.log('🔍 [UserManagement] Users type:', typeof users, Array.isArray(users));
        setUsers(Array.isArray(users) ? users : []);
      } else {
        console.error('❌ [UserManagement] Users response failed:', usersResponse);
        setError(`Failed to load users: ${usersResponse.error?.message || 'Unknown error'}`);
      }

      if (rolesResponse.success) {
        // Try different possible response structures
        const roles = rolesResponse.data?.items || [];
        console.log('🔍 [UserManagement] Extracted roles:', roles);
        console.log('🔍 [UserManagement] Roles count:', roles.length);
        console.log('🔍 [UserManagement] Roles type:', typeof roles, Array.isArray(roles));
        setRoles(Array.isArray(roles) ? roles : []);
      } else {
        console.error('❌ [UserManagement] Roles response failed:', rolesResponse);
        setError(`Failed to load roles: ${rolesResponse.error?.message || 'Unknown error'}`);
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
      // Convert roleIds to role names for the API
      const roleNames = formData.roleIds
        .map(roleId => roles.find(r => r.id === roleId)?.name)
        .filter(Boolean) as string[];
      
      const createData = {
        ...formData,
        roles: roleNames
      };
      
      console.log('🔍 [CREATE-USER] Sending data:', createData);
      const response = await apiService.createUser(createData);
      if (response.success) {
        setUsers([...users, response.data!]);
        setShowCreateForm(false);
        resetForm();
        setSuccessMessage(`User "${createData.username}" created successfully!`);
        setError(null);
        // Clear success message after 5 seconds
        setTimeout(() => setSuccessMessage(null), 5000);
      } else {
        setError('Failed to create user');
        setSuccessMessage(null);
      }
    } catch (err: any) {
      const errorMessage = err?.response?.data?.message || err?.message || 'Failed to create user';
      setError(errorMessage);
      setSuccessMessage(null);
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
      // Convert roleIds to role names for the API
      const roleNames = formData.roleIds
        .map(roleId => roles.find(r => r.id === roleId)?.name)
        .filter(Boolean) as string[];
      
      const updateData: UpdateUserRequest = {
        username: formData.username,
        email: formData.email,
        firstName: formData.firstName,
        lastName: formData.lastName,
        department: formData.department,
        roles: roleNames
      };

      console.log('🔍 [UPDATE-USER] Sending data:', updateData);
      const response = await apiService.updateUser(editingUser.id, updateData);
      if (response.success) {
        setUsers(users.map(u => u.id === editingUser.id ? response.data! : u));
        setEditingUser(null);
        resetForm();
      } else {
        setError('Failed to update user');
      }
    } catch (err: any) {
      const errorMessage = err?.response?.data?.message || err?.message || 'Failed to update user';
      setError(errorMessage);
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
    } catch (err: any) {
      const errorMessage = err?.response?.data?.message || err?.message || 'Failed to delete user';
      setError(errorMessage);
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
    
    // Basic validation only - relaxed rules
    if (password.length < 6) {
      errors.push('Password must be at least 6 characters long');
    }
    
    if (!/[a-z]/.test(password)) {
      errors.push('Password must contain at least one lowercase letter');
    }
    
    if (!/\d/.test(password)) {
      errors.push('Password must contain at least one number');
    }

    // Check for minimum unique characters
    const uniqueChars = new Set(password.toLowerCase()).size;
    if (uniqueChars < 3) {
      errors.push('Password must contain at least 3 different characters');
    }

    let strength: 'weak' | 'medium' | 'strong' = 'weak';
    if (errors.length === 0) {
      if (password.length >= 10) {
        strength = 'strong';
      } else if (password.length >= 8) {
        strength = 'medium';
      } else {
        strength = 'weak';
      }
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
    setError(null);
    setSuccessMessage(null);
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
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border-2 border-blue-500 rounded-md transition-all duration-300 hover:bg-blue-700 hover:border-blue-400 disabled:opacity-50 shadow-lg flex items-center gap-2"
            style={{ 
              backgroundColor: '#2563eb', 
              color: 'white', 
              borderColor: '#3b82f6' 
            }}
          >
            {loading ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                Loading...
              </>
            ) : (
              <>
                <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                Refresh
              </>
            )}
          </button>
          <button
            onClick={() => setShowCreateForm(true)}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border-2 border-blue-500 rounded-md transition-all duration-300 hover:bg-blue-700 hover:border-blue-400 shadow-lg flex items-center gap-2"
            style={{ 
              backgroundColor: '#2563eb', 
              color: 'white', 
              borderColor: '#3b82f6' 
            }}
          >
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 5v14M5 12h14" />
            </svg>
            Add User
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

      {successMessage && (
        <div className={`px-4 py-3 rounded mb-4 ${
          isDarkMode 
            ? 'bg-green-900/20 border border-green-500/50 text-green-400' 
            : 'bg-green-100 border border-green-400 text-green-700'
        }`}>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              {successMessage}
            </div>
            <button
              onClick={() => setSuccessMessage(null)}
              className="ml-2 text-current hover:opacity-70"
            >
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
              </svg>
            </button>
          </div>
        </div>
      )}

      {error && (
        <div className={`px-4 py-3 rounded mb-4 ${
          isDarkMode 
            ? 'bg-red-900/20 border border-red-500 text-red-300' 
            : 'bg-red-100 border border-red-400 text-red-700'
        }`}>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              <div>
                <div className="font-semibold">Error creating user:</div>
                <div className="text-sm mt-1">{error}</div>
              </div>
            </div>
            <button
              onClick={() => setError(null)}
              className="ml-2 text-current hover:opacity-70"
            >
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
              </svg>
            </button>
          </div>
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
            
            {/* Password Requirements Help */}
            {!editingUser && (
              <div className={`p-3 rounded-md text-sm ${
                isDarkMode 
                  ? 'bg-blue-900/20 border border-blue-500/30 text-blue-300' 
                  : 'bg-blue-50 border border-blue-200 text-blue-700'
              }`}>
                <div className="font-semibold mb-2">Password Requirements (Basic):</div>
                <ul className="space-y-1 text-xs">
                  <li>• At least 6 characters long</li>
                  <li>• Contains at least one lowercase letter</li>
                  <li>• Contains at least one number</li>
                  <li>• Contains at least 3 different characters</li>
                </ul>
              </div>
            )}
            <div className="space-y-2">
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                type="text"
                value={formData.username}
                onChange={(e) => {
                  setFormData({ ...formData, username: e.target.value });
                  // Re-validate password if it exists
                  if (formData.password) {
                    handlePasswordValidation(formData.password);
                  }
                }}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => {
                  setFormData({ ...formData, email: e.target.value });
                  // Re-validate password if it exists
                  if (formData.password) {
                    handlePasswordValidation(formData.password);
                  }
                }}
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
              onClick={() => {
                setShowCreateForm(false);
                setEditingUser(null);
                resetForm();
              }}
              className="bg-blue-600 hover:bg-blue-700 text-white border-2 border-blue-500 hover:border-blue-400 transition-all duration-300 shadow-lg"
              style={{ 
                backgroundColor: '#2563eb', 
                color: 'white', 
                borderColor: '#3b82f6' 
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
              className="bg-blue-600 hover:bg-blue-700 text-white border-2 border-blue-500 hover:border-blue-400 transition-all duration-300 shadow-lg disabled:opacity-50"
              style={{ 
                backgroundColor: '#2563eb', 
                color: 'white', 
                borderColor: '#3b82f6' 
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