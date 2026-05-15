import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({ name: 'markdown', standalone: false })
export class MarkdownPipe implements PipeTransform {
  constructor(private sanitizer: DomSanitizer) {}

  transform(text: string | undefined | null): SafeHtml {
    if (!text) return '';

    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    const lines = escaped.split('\n');
    const result: string[] = [];
    let inList = false;

    for (const line of lines) {
      const listMatch = line.match(/^[\-\*] (.+)$/);
      const h3 = line.match(/^### (.+)$/);
      const h2 = line.match(/^## (.+)$/);
      const h1 = line.match(/^# (.+)$/);

      if (listMatch) {
        if (!inList) { result.push('<ul>'); inList = true; }
        result.push(`<li>${listMatch[1]}</li>`);
      } else {
        if (inList) { result.push('</ul>'); inList = false; }
        if (h3)              result.push(`<strong>${h3[1]}</strong><br>`);
        else if (h2)         result.push(`<strong>${h2[1]}</strong><br>`);
        else if (h1)         result.push(`<strong>${h1[1]}</strong><br>`);
        else if (!line.trim()) result.push('<br>');
        else                 result.push(line + '<br>');
      }
    }

    if (inList) result.push('</ul>');

    const html = result.join('')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*([^\s*][^*]*[^\s*])\*/g, '<em>$1</em>')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/<br>(<\/?ul>|<li>)/g, '$1');

    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
