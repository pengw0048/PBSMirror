%% ��ʼ��
rng default
load d:\wifi\feature.dat -ascii

%% ����
cnmax=20;
sumds=zeros(cnmax,cnmax);
silhs=[];
for cn=7:7 %cnmax
    [idx,C,sumd,D]=kmeans(feature(:,2:8),cn,'display','final','replicates', 100,'Options',statset('UseParallel',1));
    save(['res' int2str(cn)],'idx','C')
    sumds(1:cn,cn)=sumd;
    [silh3,h] = silhouette(feature(200000:205000,2:8),idx(200000:205000));
    silhs=[silhs silh3];
    g = gca;
    g.Children.EdgeColor = [.8 .8 1];
    xlabel '����ϵ��'
    ylabel '�ر��'
set(gcf,'position',[200,200,400,300])
    %saveas(gcf,['res' int2str(cn) '.png'])
    %close
end

%% ������ƽ��������ϵ��ƽ��
sda=zeros(1,cnmax);
silha=zeros(1,cnmax);
for i=2:cnmax
    sda(i)=mean(sumds(1:i,i));
    silha(i)=mean(silhs(:,i-1));
end
figure
subplot(1,2,1)
hold on
box on
set(gcf,'position',[200,200,800,300])
plot(2:cnmax,sda(2:cnmax),'k','linewidth',2)
set(gca,'xlim',[2 20])
xlabel('�ص�����')
ylabel('ƽ��SSE')
subplot(1,2,2)
plot(2:cnmax,silha(2:cnmax),'k','linewidth',2)
set(gca,'xlim',[2 20])
xlabel('�ص�����')
ylabel('ƽ������ϵ��')

%% �����͸�������
mrc=zeros(2,7);
mwc=zeros(2,7);
load d:\wifi\mustright1.log -ascii
load d:\wifi\mustright2.log -ascii
load d:\wifi\mustwrong1.log -ascii
load d:\wifi\mustwrong2.log -ascii
lookup=zeros(1,1000000);
for i=1:size(feature,1)
    lookup(feature(i,1))=idx(i);
end
for i=1:size(mustright1,1)
    tidx=lookup(mustright1(i));
    if tidx==0; continue; end
    mrc(1,tidx)=mrc(1,tidx)+1;
end
for i=1:size(mustright2,1)
    tidx=lookup(mustright2(i));
    if tidx==0; continue; end
    mrc(2,tidx)=mrc(2,tidx)+1;
end
for i=1:size(mustwrong1,1)
    tidx=lookup(mustwrong1(i));
    if tidx==0; continue; end
    mwc(1,tidx)=mwc(1,tidx)+1;
end
for i=1:size(mustwrong2,1)
    tidx=lookup(mustwrong2(i));
    if tidx==0; continue; end
    mwc(2,tidx)=mwc(2,tidx)+1;
end
bp=[mrc(1,:)/sum(mrc(1,:));mrc(2,:)/sum(mrc(2,:));mwc(1,:)/sum(mwc(1,:));mwc(2,:)/sum(mwc(2,:))]';
bar(bp)
hold on
set(gcf,'position',[200,200,400,300])
xlabel '�ر��'
ylabel 'ռ�����ı���'
legend('����N_1','����N_2','����P_1','����P_2')

%% �ҳ�����α��վ��line
ys=[mustwrong1;mustwrong2;zeros(20000,1)];
ysc=size(ys,1)-20000;
for i=1:size(feature,1)
    if idx(i)==1||idx(i)==2||idx(i)==6
        ysc=ysc+1;
        ys(ysc)=feature(i,1);
    end
end
