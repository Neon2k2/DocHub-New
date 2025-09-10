import { toast } from 'sonner';

export interface NotificationOptions {
  title?: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  duration?: number;
}

export class NotificationService {
  static success(message: string, options?: NotificationOptions) {
    toast.success(message, {
      description: options?.description,
      duration: options?.duration || 4000,
      action: options?.action ? {
        label: options.action.label,
        onClick: options.action.onClick
      } : undefined
    });
  }

  static error(message: string, options?: NotificationOptions) {
    toast.error(message, {
      description: options?.description,
      duration: options?.duration || 6000,
      action: options?.action ? {
        label: options.action.label,
        onClick: options.action.onClick
      } : undefined
    });
  }

  static warning(message: string, options?: NotificationOptions) {
    toast.warning(message, {
      description: options?.description,
      duration: options?.duration || 5000,
      action: options?.action ? {
        label: options.action.label,
        onClick: options.action.onClick
      } : undefined
    });
  }

  static info(message: string, options?: NotificationOptions) {
    toast.info(message, {
      description: options?.description,
      duration: options?.duration || 4000,
      action: options?.action ? {
        label: options.action.label,
        onClick: options.action.onClick
      } : undefined
    });
  }

  static loading(message: string) {
    return toast.loading(message);
  }

  static dismiss(id?: string | number) {
    toast.dismiss(id);
  }

  // Application-specific notifications
  static documentGenerated(count: number) {
    this.success(
      `${count} document${count > 1 ? 's' : ''} generated successfully`,
      {
        description: 'Documents are ready for download or email',
        action: {
          label: 'View Documents',
          onClick: () => {
            // Navigate to documents view
            console.log('Navigate to documents view');
          }
        }
      }
    );
  }

  static emailSent(count: number) {
    this.success(
      `${count} email${count > 1 ? 's' : ''} sent successfully`,
      {
        description: 'Recipients will receive the documents shortly',
        action: {
          label: 'Track Status',
          onClick: () => {
            // Navigate to email tracking
            console.log('Navigate to email tracking');
          }
        }
      }
    );
  }

  static templateUploaded(name: string) {
    this.success(
      `Template "${name}" uploaded successfully`,
      {
        description: 'Template is now available for use',
        action: {
          label: 'Use Template',
          onClick: () => {
            // Navigate to template selection
            console.log('Navigate to template selection');
          }
        }
      }
    );
  }

  static userCreated(name: string) {
    this.success(
      `User "${name}" created successfully`,
      {
        description: 'User can now access the system',
        action: {
          label: 'View User',
          onClick: () => {
            // Navigate to user management
            console.log('Navigate to user management');
          }
        }
      }
    );
  }

  static settingsSaved() {
    this.success(
      'Settings saved successfully',
      {
        description: 'Your changes have been applied',
        duration: 3000
      }
    );
  }

  static operationFailed(operation: string, error?: string) {
    this.error(
      `${operation} failed`,
      {
        description: error || 'Please try again or contact support',
        duration: 6000
      }
    );
  }

  static confirmAction(
    message: string,
    onConfirm: () => void,
    onCancel?: () => void
  ) {
    toast(message, {
      description: 'This action cannot be undone',
      action: {
        label: 'Confirm',
        onClick: onConfirm
      },
      cancel: {
        label: 'Cancel',
        onClick: onCancel || (() => {})
      },
      duration: 10000
    });
  }
}

export const notify = NotificationService;
