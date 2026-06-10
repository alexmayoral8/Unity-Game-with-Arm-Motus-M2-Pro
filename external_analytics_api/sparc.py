import numpy as np


def sparc(movement, fs, padlevel=4, fc=10.0, amp_th=0.05):
    """
    Calculates the smoothness of the given speed profile using the modified
    spectral arc length metric.
    """
    movement = np.asarray(movement, dtype=float)

    nfft = int(pow(2, np.ceil(np.log2(len(movement))) + padlevel))

    f = np.arange(0, fs, fs / nfft)
    mf = abs(np.fft.fft(movement, nfft))
    mf = mf / max(mf)

    fc_inx = ((f <= fc) * 1).nonzero()
    f_sel = f[fc_inx]
    mf_sel = mf[fc_inx]

    inx = ((mf_sel >= amp_th) * 1).nonzero()[0]
    fc_inx = range(inx[0], inx[-1] + 1)
    f_sel = f_sel[fc_inx]
    mf_sel = mf_sel[fc_inx]

    new_sal = -sum(
        np.sqrt(
            pow(np.diff(f_sel) / (f_sel[-1] - f_sel[0]), 2)
            + pow(np.diff(mf_sel), 2)
        )
    )
    return new_sal, (f, mf), (f_sel, mf_sel)


def safe_sparc(movement, fs, padlevel=4, fc=10.0, amp_th=0.05):
    movement = np.asarray(movement, dtype=float)
    movement = movement[np.isfinite(movement)]

    if len(movement) < 3:
        return float("nan")

    if np.nanmax(np.abs(movement)) == 0:
        return float("nan")

    try:
        value, _, _ = sparc(
            movement,
            fs=fs,
            padlevel=padlevel,
            fc=fc,
            amp_th=amp_th,
        )
    except (FloatingPointError, IndexError, ValueError, ZeroDivisionError):
        return float("nan")

    return float(value)
