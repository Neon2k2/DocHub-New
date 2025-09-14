using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Microsoft.Extensions.Logging;

namespace DocHub.Application.Services;

public interface ISignatureCleanupService
{
    byte[] CleanupSignature(byte[] originalImageBytes, string fileName);
    byte[] CleanupSignatureAdvanced(byte[] originalImageBytes, string fileName);
    byte[] CleanupSignatureUltraAggressive(byte[] originalImageBytes, string fileName);
    bool IsImageFile(string fileName);
}

public class SignatureCleanupService : ISignatureCleanupService
{
    private readonly ILogger<SignatureCleanupService> _logger;

    public SignatureCleanupService(ILogger<SignatureCleanupService> logger)
    {
        _logger = logger;
    }

    public bool IsImageFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" => true,
            _ => false
        };
    }

    public byte[] CleanupSignature(byte[] originalImageBytes, string fileName)
    {
        try
        {
            if (!IsImageFile(fileName))
            {
                _logger.LogWarning("File {FileName} is not a supported image format", fileName);
                return originalImageBytes;
            }

            using var originalImage = Image.FromStream(new MemoryStream(originalImageBytes));
            
            // Create a new image with transparent background
            using var cleanedImage = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(cleanedImage))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Apply enhanced cleanup filters specifically for Adobe watermarks
                var cleanedBitmap = ApplyEnhancedCleanup(originalImage);
                
                // Draw the cleaned image
                graphics.DrawImage(cleanedBitmap, 0, 0, originalImage.Width, originalImage.Height);
            }

            // Convert to PNG with transparency
            using var outputStream = new MemoryStream();
            cleanedImage.Save(outputStream, ImageFormat.Png);
            
            _logger.LogInformation("Successfully cleaned signature image {FileName} with enhanced Adobe watermark removal", fileName);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning signature image {FileName}", fileName);
            // Return original image if cleanup fails
            return originalImageBytes;
        }
    }

    private Bitmap ApplyEnhancedCleanup(Image originalImage)
    {
        var bitmap = new Bitmap(originalImage);
        var cleanedBitmap = new Bitmap(bitmap.Width, bitmap.Height);

        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                var pixel = bitmap.GetPixel(x, y);
                
                // Enhanced Adobe watermark detection and removal
                if (IsAdobeWatermark(pixel, bitmap, x, y))
                {
                    cleanedBitmap.SetPixel(x, y, Color.Transparent);
                }
                // Remove light backgrounds more aggressively
                else if (IsLightBackground(pixel))
                {
                    cleanedBitmap.SetPixel(x, y, Color.Transparent);
                }
                // Keep and enhance signature content
                else if (IsSignatureContent(pixel))
                {
                    var enhancedColor = EnhanceSignatureColor(pixel);
                    cleanedBitmap.SetPixel(x, y, enhancedColor);
                }
                else
                {
                    // Keep other colors but make them more transparent
                    var semiTransparentColor = Color.FromArgb(
                        (int)(pixel.A * 0.8), // Reduce opacity
                        pixel.R, pixel.G, pixel.B
                    );
                    cleanedBitmap.SetPixel(x, y, semiTransparentColor);
                }
            }
        }

        return cleanedBitmap;
    }

    private bool IsAdobeWatermark(Color pixel, Bitmap bitmap, int x, int y)
    {
        // Adobe watermark detection - multiple criteria
        
        // 1. Adobe logo red color detection (more precise)
        if (IsAdobeRedColor(pixel))
            return true;
        
        // 2. Check for Adobe PDF icon colors (translucent red)
        if (IsAdobePdfIconColor(pixel))
            return true;
        
        // 3. Check for Adobe signature stamp colors
        if (IsAdobeSignatureStampColor(pixel))
            return true;
        
        // 4. Enhanced reddish-brown watermark detection
        if (IsAdobeReddishBrownWatermark(pixel))
            return true;
        
        // 5. Pattern detection - check surrounding pixels for Adobe watermark patterns
        if (IsAdobeWatermarkPattern(bitmap, x, y))
            return true;
        
        return false;
    }

    private bool IsAdobeRedColor(Color pixel)
    {
        // Adobe's signature red color: RGB(255, 0, 0) or close variations
        // Also detect Adobe's translucent red overlay colors
        var redDominance = pixel.R - Math.Max(pixel.G, pixel.B);
        
        // Primary Adobe red detection
        if (pixel.R > 220 && pixel.G < 80 && pixel.B < 80 && redDominance > 140)
            return true;
        
        // Adobe translucent red overlay detection
        if (pixel.R > 200 && pixel.G < 120 && pixel.B < 120 && redDominance > 80)
        {
            // Check if it's a translucent overlay (not solid)
            var totalBrightness = (pixel.R + pixel.G + pixel.B) / 3.0;
            if (totalBrightness > 150) // Translucent overlay is usually lighter
                return true;
        }
        
        return false;
    }

    private bool IsAdobePdfIconColor(Color pixel)
    {
        // Adobe PDF icon has specific color characteristics
        // Usually a translucent red with slight pink tint
        if (pixel.R > 180 && pixel.G > 80 && pixel.G < 150 && pixel.B > 80 && pixel.B < 150)
        {
            var redDominance = pixel.R - Math.Max(pixel.G, pixel.B);
            return redDominance > 30 && redDominance < 100; // Adobe PDF icon range
        }
        return false;
    }

    private bool IsAdobeSignatureStampColor(Color pixel)
    {
        // Adobe digital signature stamp colors
        // Often has a reddish-brown or burgundy tint
        if (pixel.R > 160 && pixel.G > 60 && pixel.G < 140 && pixel.B > 60 && pixel.B < 140)
        {
            var redDominance = pixel.R - Math.Max(pixel.G, pixel.B);
            var greenBlueDiff = Math.Abs(pixel.G - pixel.B);
            
            // Adobe signature stamp has red dominance but green/blue are closer together
            return redDominance > 20 && redDominance < 80 && greenBlueDiff < 30;
        }
        return false;
    }

    private bool IsAdobeReddishBrownWatermark(Color pixel)
    {
        // Enhanced detection for the specific reddish-brown Adobe watermarks
        // that are still visible in processed images
        
        var redDominance = pixel.R - Math.Max(pixel.G, pixel.B);
        var totalBrightness = (pixel.R + pixel.G + pixel.B) / 3.0;
        
        // Detect reddish-brown colors with various intensity levels
        if (pixel.R > 140 && pixel.G > 40 && pixel.G < 120 && pixel.B > 40 && pixel.B < 120)
        {
            // Check for reddish-brown characteristics
            if (redDominance > 15 && redDominance < 100)
            {
                // Additional check for watermark-like appearance
                var colorVariation = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B) - Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
                
                // Watermarks often have moderate color variation
                if (colorVariation > 20 && colorVariation < 80)
                {
                    // Check if it's not too dark (watermarks are usually medium brightness)
                    if (totalBrightness > 80 && totalBrightness < 200)
                    {
                        return true;
                    }
                }
            }
        }
        
        // More aggressive detection for any reddish tint
        if (pixel.R > 120 && redDominance > 10)
        {
            // Check if green and blue are relatively close (indicating brownish tint)
            var greenBlueDiff = Math.Abs(pixel.G - pixel.B);
            if (greenBlueDiff < 40 && pixel.G > 30 && pixel.B > 30)
            {
                // Additional check for semi-transparent watermark appearance
                var saturation = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B) - Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
                if (saturation > 15 && saturation < 70)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private bool IsAdobeWatermarkPattern(Bitmap bitmap, int x, int y)
    {
        // Check for Adobe watermark patterns in surrounding area
        // Adobe watermarks often have specific spatial patterns
        
        if (x < 2 || y < 2 || x >= bitmap.Width - 2 || y >= bitmap.Height - 2)
            return false;
        
        var centerPixel = bitmap.GetPixel(x, y);
        var neighbors = GetNeighborPixels(bitmap, x, y);
        
        // Check if center pixel is part of Adobe watermark pattern
        // Adobe watermarks often have consistent color patterns in small regions
        
        var adobeColorCount = 0;
        foreach (var neighbor in neighbors)
        {
            if (IsAdobeRedColor(neighbor) || IsAdobePdfIconColor(neighbor) || IsAdobeSignatureStampColor(neighbor))
                adobeColorCount++;
        }
        
        // If multiple surrounding pixels are Adobe colors, this is likely part of watermark
        return adobeColorCount >= 3;
    }

    private bool IsLightBackground(Color pixel)
    {
        // Enhanced light background detection
        var brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
        
        // More aggressive threshold for better background removal
        if (brightness > 230) // Very light threshold
            return true;
        
        // Also detect near-white colors with slight tint
        if (brightness > 200)
        {
            var maxDiff = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B) - Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
            return maxDiff < 30; // Very low color variation indicates near-white
        }
        
        return false;
    }

    private bool IsSignatureContent(Color pixel)
    {
        // Enhanced signature content detection
        var brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
        
        // Signature content is typically dark
        if (brightness < 120)
            return true;
        
        // Also detect medium-dark colors that might be part of signature
        if (brightness < 180)
        {
            // Check for good contrast (not washed out)
            var contrast = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B) - Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
            return contrast > 40; // Good contrast indicates signature content
        }
        
        return false;
    }

    private Color EnhanceSignatureColor(Color pixel)
    {
        // Enhance signature colors for better visibility
        var brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
        
        if (brightness < 80)
        {
            // Very dark pixels - make them pure black for crisp signature
            return Color.FromArgb(pixel.A, 0, 0, 0);
        }
        else if (brightness < 150)
        {
            // Medium-dark pixels - darken them for better contrast
            var factor = 0.6; // Darken by 40%
            var r = (int)(pixel.R * factor);
            var g = (int)(pixel.G * factor);
            var b = (int)(pixel.B * factor);
            
            return Color.FromArgb(pixel.A, r, g, b);
        }
        else
        {
            // Keep original color but ensure good contrast
            return pixel;
        }
    }

    private Color[] GetNeighborPixels(Bitmap bitmap, int x, int y)
    {
        return new[]
        {
            bitmap.GetPixel(x - 1, y - 1),
            bitmap.GetPixel(x, y - 1),
            bitmap.GetPixel(x + 1, y - 1),
            bitmap.GetPixel(x - 1, y),
            bitmap.GetPixel(x + 1, y),
            bitmap.GetPixel(x - 1, y + 1),
            bitmap.GetPixel(x, y + 1),
            bitmap.GetPixel(x + 1, y + 1)
        };
    }

    // Alternative method using more advanced image processing
    public byte[] CleanupSignatureAdvanced(byte[] originalImageBytes, string fileName)
    {
        try
        {
            using var originalImage = Image.FromStream(new MemoryStream(originalImageBytes));
            
            // Create a new image with transparent background
            using var cleanedImage = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(cleanedImage))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Apply advanced cleanup with Adobe watermark focus
                var cleanedBitmap = ApplyAdvancedCleanup(originalImage);
                graphics.DrawImage(cleanedBitmap, 0, 0, originalImage.Width, originalImage.Height);
            }

            using var outputStream = new MemoryStream();
            cleanedImage.Save(outputStream, ImageFormat.Png);
            
            _logger.LogInformation("Successfully applied advanced cleanup to signature {FileName} with Adobe watermark removal", fileName);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying advanced cleanup to signature {FileName}", fileName);
            return originalImageBytes;
        }
    }

    private Bitmap ApplyAdvancedCleanup(Image originalImage)
    {
        var bitmap = new Bitmap(originalImage);
        var cleanedBitmap = new Bitmap(bitmap.Width, bitmap.Height);

        // Edge detection and noise reduction with Adobe watermark focus
        for (int x = 1; x < bitmap.Width - 1; x++)
        {
            for (int y = 1; y < bitmap.Height - 1; y++)
            {
                var centerPixel = bitmap.GetPixel(x, y);
                var neighbors = GetNeighborPixels(bitmap, x, y);
                
                // Enhanced edge detection for signature lines
                if (IsSignatureEdge(centerPixel, neighbors))
                {
                    // Keep and enhance signature edges
                    var enhancedColor = Color.FromArgb(255, 0, 0, 0); // Pure black
                    cleanedBitmap.SetPixel(x, y, enhancedColor);
                }
                // Remove Adobe watermarks and noise
                else if (IsAdobeWatermark(centerPixel, bitmap, x, y) || IsNoiseOrWatermark(centerPixel, neighbors))
                {
                    cleanedBitmap.SetPixel(x, y, Color.Transparent);
                }
                else
                {
                    // Keep other pixels but reduce opacity
                    var semiTransparentColor = Color.FromArgb(
                        (int)(centerPixel.A * 0.7), // Reduce opacity more
                        centerPixel.R, centerPixel.G, centerPixel.B
                    );
                    cleanedBitmap.SetPixel(x, y, semiTransparentColor);
                }
            }
        }

        return cleanedBitmap;
    }

    private bool IsSignatureEdge(Color center, Color[] neighbors)
    {
        var centerBrightness = (center.R + center.G + center.B) / 3.0;
        var neighborBrightness = neighbors.Select(n => (n.R + n.G + n.B) / 3.0).ToArray();
        
        // Enhanced edge detection for signature lines
        var maxDiff = neighborBrightness.Max(b => Math.Abs(b - centerBrightness));
        
        // Signature edges typically have high contrast
        if (maxDiff > 60) // Higher threshold for signature edges
        {
            // Additional check: ensure center pixel is dark (signature content)
            return centerBrightness < 150;
        }
        
        return false;
    }

    private bool IsNoiseOrWatermark(Color center, Color[] neighbors)
    {
        // Enhanced noise and watermark detection
        var centerBrightness = (center.R + center.G + center.B) / 3.0;
        var neighborBrightness = neighbors.Select(n => (n.R + n.G + n.B) / 3.0).ToArray();
        
        // Check for Adobe watermark colors first
        if (IsAdobeRedColor(center) || IsAdobePdfIconColor(center) || IsAdobeSignatureStampColor(center))
            return true;
        
        // Check for isolated pixels (noise)
        var avgNeighborBrightness = neighborBrightness.Average();
        var isolation = Math.Abs(centerBrightness - avgNeighborBrightness);
        
        // More aggressive noise removal
        return isolation > 80; // Lower threshold for better noise removal
    }

    // Ultra-aggressive watermark removal for stubborn Adobe watermarks
    public byte[] CleanupSignatureUltraAggressive(byte[] originalImageBytes, string fileName)
    {
        try
        {
            if (!IsImageFile(fileName))
            {
                _logger.LogWarning("File {FileName} is not a supported image format", fileName);
                return originalImageBytes;
            }

            using var originalImage = Image.FromStream(new MemoryStream(originalImageBytes));
            
            // Create a new image with transparent background
            using var cleanedImage = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(cleanedImage))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Apply ultra-aggressive cleanup
                var cleanedBitmap = ApplyUltraAggressiveCleanup(originalImage);
                graphics.DrawImage(cleanedBitmap, 0, 0, originalImage.Width, originalImage.Height);
            }

            // Convert to PNG with transparency
            using var outputStream = new MemoryStream();
            cleanedImage.Save(outputStream, ImageFormat.Png);
            
            _logger.LogInformation("Successfully applied ultra-aggressive cleanup to signature {FileName} with complete Adobe watermark removal", fileName);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying ultra-aggressive cleanup to signature {FileName}", fileName);
            return originalImageBytes;
        }
    }

    private Bitmap ApplyUltraAggressiveCleanup(Image originalImage)
    {
        var bitmap = new Bitmap(originalImage);
        var cleanedBitmap = new Bitmap(bitmap.Width, bitmap.Height);

        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                var pixel = bitmap.GetPixel(x, y);
                
                // Ultra-aggressive Adobe watermark detection
                if (IsAdobeWatermarkUltraAggressive(pixel))
                {
                    cleanedBitmap.SetPixel(x, y, Color.Transparent);
                }
                // Remove light backgrounds very aggressively
                else if (IsLightBackgroundUltraAggressive(pixel))
                {
                    cleanedBitmap.SetPixel(x, y, Color.Transparent);
                }
                // Keep only very dark signature content
                else if (IsSignatureContentUltraAggressive(pixel))
                {
                    var enhancedColor = EnhanceSignatureColorUltraAggressive(pixel);
                    cleanedBitmap.SetPixel(x, y, enhancedColor);
                }
                else
                {
                    // Remove everything else
                    cleanedBitmap.SetPixel(x, y, Color.Transparent);
                }
            }
        }

        return cleanedBitmap;
    }

    private bool IsAdobeWatermarkUltraAggressive(Color pixel)
    {
        // Ultra-aggressive Adobe watermark detection
        var redDominance = pixel.R - Math.Max(pixel.G, pixel.B);
        var totalBrightness = (pixel.R + pixel.G + pixel.B) / 3.0;
        
        // Remove ANY pixel with significant red dominance
        if (redDominance > 5)
        {
            // Check if it's not pure black (signature content)
            if (totalBrightness > 30)
            {
                return true;
            }
        }
        
        // Remove any reddish-brown colors
        if (pixel.R > 100 && pixel.G > 20 && pixel.B > 20)
        {
            if (redDominance > 3)
            {
                return true;
            }
        }
        
        // Remove any medium-brightness pixels with color variation (likely watermarks)
        if (totalBrightness > 60 && totalBrightness < 180)
        {
            var colorVariation = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B) - Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
            if (colorVariation > 10 && colorVariation < 100)
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsLightBackgroundUltraAggressive(Color pixel)
    {
        var brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
        
        // Very aggressive threshold - remove anything that's not very dark
        if (brightness > 150)
            return true;
        
        // Also remove pixels with low contrast
        var maxDiff = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B) - Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
        if (maxDiff < 50)
            return true;
        
        return false;
    }

    private bool IsSignatureContentUltraAggressive(Color pixel)
    {
        var brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
        
        // Only keep very dark pixels (likely signature content)
        if (brightness < 80)
        {
            // Ensure good contrast
            var contrast = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B) - Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
            return contrast > 30;
        }
        
        return false;
    }

    private Color EnhanceSignatureColorUltraAggressive(Color pixel)
    {
        // Make signature pure black for maximum contrast
        return Color.FromArgb(255, 0, 0, 0);
    }
}