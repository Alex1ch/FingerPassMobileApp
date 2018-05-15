package md5c53ccc5d59c9b7ec46518d14442feecd;


public class AuthCallback
	extends android.support.v4.hardware.fingerprint.FingerprintManagerCompat.AuthenticationCallback
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onAuthenticationSucceeded:(Landroid/support/v4/hardware/fingerprint/FingerprintManagerCompat$AuthenticationResult;)V:GetOnAuthenticationSucceeded_Landroid_support_v4_hardware_fingerprint_FingerprintManagerCompat_AuthenticationResult_Handler\n" +
			"n_onAuthenticationError:(ILjava/lang/CharSequence;)V:GetOnAuthenticationError_ILjava_lang_CharSequence_Handler\n" +
			"n_onAuthenticationFailed:()V:GetOnAuthenticationFailedHandler\n" +
			"n_onAuthenticationHelp:(ILjava/lang/CharSequence;)V:GetOnAuthenticationHelp_ILjava_lang_CharSequence_Handler\n" +
			"";
		mono.android.Runtime.register ("FingerPass.AuthCallback, FingerPass, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", AuthCallback.class, __md_methods);
	}


	public AuthCallback ()
	{
		super ();
		if (getClass () == AuthCallback.class)
			mono.android.TypeManager.Activate ("FingerPass.AuthCallback, FingerPass, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	public AuthCallback (md5c53ccc5d59c9b7ec46518d14442feecd.Auth p0)
	{
		super ();
		if (getClass () == AuthCallback.class)
			mono.android.TypeManager.Activate ("FingerPass.AuthCallback, FingerPass, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "FingerPass.Auth, FingerPass, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", this, new java.lang.Object[] { p0 });
	}


	public void onAuthenticationSucceeded (android.support.v4.hardware.fingerprint.FingerprintManagerCompat.AuthenticationResult p0)
	{
		n_onAuthenticationSucceeded (p0);
	}

	private native void n_onAuthenticationSucceeded (android.support.v4.hardware.fingerprint.FingerprintManagerCompat.AuthenticationResult p0);


	public void onAuthenticationError (int p0, java.lang.CharSequence p1)
	{
		n_onAuthenticationError (p0, p1);
	}

	private native void n_onAuthenticationError (int p0, java.lang.CharSequence p1);


	public void onAuthenticationFailed ()
	{
		n_onAuthenticationFailed ();
	}

	private native void n_onAuthenticationFailed ();


	public void onAuthenticationHelp (int p0, java.lang.CharSequence p1)
	{
		n_onAuthenticationHelp (p0, p1);
	}

	private native void n_onAuthenticationHelp (int p0, java.lang.CharSequence p1);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
