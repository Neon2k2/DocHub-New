import React, { useState } from 'react';
import { Button } from './ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from './ui/dialog';

export function TestDialog() {
  const [open, setOpen] = useState(false);

  return (
    <div className="p-4">
      <Button onClick={() => setOpen(true)}>Open Test Dialog</Button>
      
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="dialog-panel">
          <DialogHeader>
            <DialogTitle>Test Dialog</DialogTitle>
            <DialogDescription>
              This is a test dialog to verify the dialog system is working.
            </DialogDescription>
          </DialogHeader>
          <div className="p-4">
            <p>Dialog content is visible!</p>
            <Button onClick={() => setOpen(false)} className="mt-4">
              Close
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}