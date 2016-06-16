/*

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using iText.IO.Codec;
using iText.Kernel;
using iText.Kernel.Pdf;

namespace iText.Kernel.Pdf.Filters {
    /// <summary>Handles CCITTFaxDecode filter</summary>
    public class CCITTFaxDecodeFilter : IFilterHandler {
        public virtual byte[] Decode(byte[] b, PdfName filterName, PdfObject decodeParams, PdfDictionary streamDictionary
            ) {
            PdfNumber wn = streamDictionary.GetAsNumber(PdfName.Width);
            PdfNumber hn = streamDictionary.GetAsNumber(PdfName.Height);
            if (wn == null || hn == null) {
                throw new PdfException(PdfException.FilterCcittfaxdecodeIsOnlySupportedForImages);
            }
            int width = wn.IntValue();
            int height = hn.IntValue();
            PdfDictionary param = decodeParams is PdfDictionary ? (PdfDictionary)decodeParams : null;
            int k = 0;
            bool blackIs1 = false;
            bool byteAlign = false;
            if (param != null) {
                PdfNumber kn = param.GetAsNumber(PdfName.K);
                if (kn != null) {
                    k = kn.IntValue();
                }
                PdfBoolean bo = param.GetAsBoolean(PdfName.BlackIs1);
                if (bo != null) {
                    blackIs1 = bo.GetValue();
                }
                bo = param.GetAsBoolean(PdfName.EncodedByteAlign);
                if (bo != null) {
                    byteAlign = bo.GetValue();
                }
            }
            byte[] outBuf = new byte[(width + 7) / 8 * height];
            TIFFFaxDecompressor decoder = new TIFFFaxDecompressor();
            if (k == 0 || k > 0) {
                int tiffT4Options = k > 0 ? TIFFConstants.GROUP3OPT_2DENCODING : 0;
                tiffT4Options |= byteAlign ? TIFFConstants.GROUP3OPT_FILLBITS : 0;
                decoder.SetOptions(1, TIFFConstants.COMPRESSION_CCITTFAX3, tiffT4Options, 0);
                decoder.DecodeRaw(outBuf, b, width, height);
                if (decoder.fails > 0) {
                    byte[] outBuf2 = new byte[(width + 7) / 8 * height];
                    int oldFails = decoder.fails;
                    decoder.SetOptions(1, TIFFConstants.COMPRESSION_CCITTRLE, tiffT4Options, 0);
                    decoder.DecodeRaw(outBuf2, b, width, height);
                    if (decoder.fails < oldFails) {
                        outBuf = outBuf2;
                    }
                }
            }
            else {
                TIFFFaxDecoder deca = new TIFFFaxDecoder(1, width, height);
                deca.DecodeT6(outBuf, b, 0, height, 0);
            }
            if (!blackIs1) {
                int len = outBuf.Length;
                for (int t = 0; t < len; ++t) {
                    outBuf[t] ^= 0xff;
                }
            }
            b = outBuf;
            return b;
        }
    }
}
