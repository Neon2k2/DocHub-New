import React, { useState, useEffect } from 'react';
import { Upload, Image as ImageIcon, Trash2, Edit, Plus, Download } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '../ui/dialog';
import { Badge } from '../ui/badge';
import { Loading } from '../ui/loading';
import { documentService, Signature } from '../../services/document.service';
import { notify } from '../../utils/notifications';
import { handleError } from '../../utils/errorHandler';

export function SignatureManagement() {
  const [signatures, setSignatures] = useState<Signature[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [showUploadDialog, setShowUploadDialog] = useState(false);
  const [uploadForm, setUploadForm] = useState({
    name: '',
    file: null as File | null
  });

  useEffect(() => {
    loadSignatures();
  }, []);

  const loadSignatures = async () => {
    setLoading(true);
    try {
      const data = await documentService.getSignatures();
      setSignatures(data);
    } catch (error) {
      handleError(error, 'Load signatures');
    } finally {
      setLoading(false);
    }
  };

  const handleUploadSignature = async () => {
    if (!uploadForm.name || !uploadForm.file) {
      notify.error('Please provide both signature name and file');
      return;
    }

    setUploading(true);
    try {
      const newSignature = await documentService.uploadSignature(uploadForm.file, uploadForm.name);
      setSignatures(prev => [...prev, newSignature]);
      setUploadForm({ name: '', file: null });
      setShowUploadDialog(false);
      notify.success('Signature uploaded successfully');
    } catch (error) {
      handleError(error, 'Upload signature');
    } finally {
      setUploading(false);
    }
  };

  const handleDeleteSignature = async (signatureId: string) => {
    const signature = signatures.find(s => s.id === signatureId);
    if (!signature) return;

    notify.confirmAction(
      `Are you sure you want to delete signature "${signature.name}"?`,
      async () => {
        try {
          // In a real implementation, you would call an API to delete the signature
          setSignatures(prev => prev.filter(s => s.id !== signatureId));
          notify.success('Signature deleted successfully');
        } catch (error) {
          handleError(error, 'Delete signature');
        }
      }
    );
  };



  const handleDownloadSignature = (signature: Signature) => {
    try {
      // Create a download link for the signature
      const link = document.createElement('a');
      link.href = signature.url;
      link.download = `${signature.name}.png`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      
      notify.success('Signature downloaded successfully');
    } catch (error) {
      handleError(error, 'Download signature');
    }
  };

  const resetForm = () => {
    setUploadForm({ name: '', file: null });
    setEditingSignature(null);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Signature Management</h1>
          <p className="text-muted-foreground">Manage digital signatures for document generation</p>
        </div>
        
        <Dialog open={showUploadDialog} onOpenChange={(open) => {
          setShowUploadDialog(open);
          if (!open) resetForm();
        }}>
          <DialogTrigger asChild>
            <Button className="border border-blue-500 bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900 transition-all duration-300">
              <Plus className="h-4 w-4 mr-2" />
              Upload Signature
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle>
                Upload New Signature
              </DialogTitle>
              <DialogDescription>
                Add a new digital signature to the system
              </DialogDescription>
            </DialogHeader>
            
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="signature-name">Signature Name</Label>
                <Input
                  id="signature-name"
                  value={uploadForm.name}
                  onChange={(e) => setUploadForm(prev => ({ ...prev, name: e.target.value }))}
                  placeholder="Enter signature name"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="signature-file">Signature File</Label>
                <Input
                  id="signature-file"
                  type="file"
                  accept="image/*,.png,.jpg,.jpeg"
                  onChange={(e) => setUploadForm(prev => ({ ...prev, file: e.target.files?.[0] || null }))}
                  required
                />
                <p className="text-xs text-muted-foreground">
                  Supported formats: PNG, JPG, JPEG (max 2MB)
                </p>
              </div>
              
              <div className="flex gap-3 pt-4">
                <Button 
                  onClick={handleUploadSignature}
                  disabled={uploading || !uploadForm.name || !uploadForm.file}
                  className="flex-1"
                >
                  {uploading ? (
                    <Loading size="sm" className="mr-2" />
                  ) : (
                    <Upload className="h-4 w-4 mr-2" />
                  )}
                  {uploading ? 'Processing...' : 'Upload'}
                </Button>
                <Button 
                  variant="outline" 
                  onClick={() => setShowUploadDialog(false)}
                  className="flex-1"
                >
                  Cancel
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      {/* Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total Signatures</p>
                <p className="text-2xl font-bold">{signatures.length}</p>
              </div>
              <ImageIcon className="h-8 w-8 text-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Active Signatures</p>
                <p className="text-2xl font-bold">{signatures.filter(s => s.isActive).length}</p>
              </div>
              <ImageIcon className="h-8 w-8 text-green-400" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Last Updated</p>
                <p className="text-2xl font-bold">
                  {signatures.length > 0 ? 
                    new Date(Math.max(...signatures.map(s => new Date(s.uploadedAt).getTime()))).toLocaleDateString() :
                    'N/A'
                  }
                </p>
              </div>
              <ImageIcon className="h-8 w-8 text-orange-400" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Signatures Grid */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle>Digital Signatures</CardTitle>
          <CardDescription>
            Manage and organize your digital signatures
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <Loading text="Loading signatures..." />
            </div>
          ) : signatures.length === 0 ? (
            <div className="text-center py-12">
              <ImageIcon className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">No Signatures Found</h3>
              <p className="text-muted-foreground mb-6">
                Upload your first digital signature to get started
              </p>
              <Button onClick={() => setShowUploadDialog(true)}>
                <Plus className="h-4 w-4 mr-2" />
                Upload Signature
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {signatures.map((signature) => (
                <Card key={signature.id} className="hover:bg-muted/10 transition-colors">
                  <CardContent className="p-4">
                    <div className="space-y-4">
                      {/* Signature Preview */}
                      <div className="aspect-video bg-muted rounded-lg flex items-center justify-center">
                        <img
                          src={signature.url}
                          alt={signature.name}
                          className="max-h-full max-w-full object-contain"
                          onError={(e) => {
                            (e.target as HTMLImageElement).style.display = 'none';
                            (e.target as HTMLImageElement).nextElementSibling?.classList.remove('hidden');
                          }}
                        />
                        <div className="hidden text-muted-foreground text-center">
                          <ImageIcon className="h-8 w-8 mx-auto mb-2" />
                          <p className="text-sm">Preview not available</p>
                        </div>
                      </div>
                      
                      {/* Signature Info */}
                      <div className="space-y-2">
                        <div className="flex items-center justify-between">
                          <h3 className="font-semibold truncate">{signature.name}</h3>
                          <Badge variant={signature.isActive ? 'default' : 'secondary'}>
                            {signature.isActive ? 'Active' : 'Inactive'}
                          </Badge>
                        </div>
                        
                        <div className="text-sm text-muted-foreground">
                          <p>Uploaded: {new Date(signature.uploadedAt).toLocaleDateString()}</p>
                          <p>Size: {signature.fileSize ? `${(signature.fileSize / 1024).toFixed(1)} KB` : 'Unknown'}</p>
                        </div>
                      </div>
                      
                      {/* Actions */}
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleDownloadSignature(signature)}
                          className="flex-1"
                        >
                          <Download className="h-3 w-3 mr-1" />
                          Download
                        </Button>
                        
                        
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleDeleteSignature(signature.id)}
                          className="text-red-400 hover:text-red-300"
                        >
                          <Trash2 className="h-3 w-3" />
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
