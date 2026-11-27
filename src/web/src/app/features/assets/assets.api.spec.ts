import { nominalBand, toQueryParams } from './assets.api';

describe('assets.api', () => {
  it('leaves empty filters out of the query string and trims the search text', () => {
    expect(toQueryParams({ lineId: '', criticality: '', search: '  ' })).toEqual({});
    expect(toQueryParams({ lineId: 'l1', criticality: 'High', search: ' mtr ' })).toEqual({
      lineId: 'l1',
      criticality: 'High',
      search: 'mtr',
    });
  });

  it('formats the nominal band for closed, half-open and missing ranges', () => {
    expect(nominalBand({ nominalMin: 20, nominalMax: 80, unit: '°C' })).toBe('20–80 °C');
    expect(nominalBand({ nominalMin: 5, nominalMax: null, unit: 'bar' })).toBe('≥ 5 bar');
    expect(nominalBand({ nominalMin: null, nominalMax: 3, unit: 'mm/s' })).toBe('≤ 3 mm/s');
    expect(nominalBand({ nominalMin: null, nominalMax: null, unit: 'A' })).toBe('—');
  });
});
